using System;

namespace Assets.Scripts.System.Compression
{
    // Pure C# LZO1X / LZO1Y decompressor (port of the reference lzo1x_d.ch logic).
    // Replaces the old lzo2.dll P/Invoke so the game data loads on macOS and Linux
    // without any native plugin. Bounds checked: corrupt data throws instead of
    // writing out of range.
    public static class LZO
    {
        public static uint Decompress(byte[] compressedData, byte[] decompressedData, uint decompresedLength, CompressionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case CompressionAlgorithm.LZO1X:
                    return (uint)DecompressCore(compressedData, compressedData.Length, decompressedData, false);
                case CompressionAlgorithm.LZO1Y:
                    return (uint)DecompressCore(compressedData, compressedData.Length, decompressedData, true);
                default:
                    throw new ArgumentOutOfRangeException("algorithm");
            }
        }

        private static Exception Corrupt(string why)
        {
            return new Exception("Failure decompressing data: " + why);
        }

        private static int DecompressCore(byte[] src, int srcLen, byte[] dst, bool lzo1y)
        {
            int ip = 0;
            int op = 0;
            int dstLen = dst.Length;
            int m2MaxOffset = lzo1y ? 0x0400 : 0x0800;
            int t;
            int mPos;
            int state; // 0 = outer loop, 1 = first_literal_run, 2 = match, 3 = match_next

            if (srcLen == 0)
                throw Corrupt("empty input");

            if (src[ip] > 17)
            {
                t = src[ip++] - 17;
                if (t < 4)
                {
                    state = 3;
                }
                else
                {
                    if (ip + t > srcLen) throw Corrupt("input overrun");
                    if (op + t > dstLen) throw Corrupt("output overrun");
                    Buffer.BlockCopy(src, ip, dst, op, t);
                    ip += t;
                    op += t;
                    state = 1;
                }
            }
            else
            {
                t = 0;
                state = 0;
            }

            while (true)
            {
                if (state == 0)
                {
                    // outer loop: literal run
                    if (ip >= srcLen) throw Corrupt("input overrun");
                    t = src[ip++];
                    if (t >= 16)
                    {
                        state = 2;
                        continue;
                    }
                    if (t == 0)
                    {
                        while (true)
                        {
                            if (ip >= srcLen) throw Corrupt("input overrun");
                            if (src[ip] != 0) break;
                            t += 255;
                            ip++;
                        }
                        t += 15 + src[ip++];
                    }
                    int lit = t + 3;
                    if (ip + lit > srcLen) throw Corrupt("input overrun");
                    if (op + lit > dstLen) throw Corrupt("output overrun");
                    Buffer.BlockCopy(src, ip, dst, op, lit);
                    ip += lit;
                    op += lit;
                    state = 1;
                    continue;
                }

                if (state == 1)
                {
                    // first_literal_run
                    if (ip >= srcLen) throw Corrupt("input overrun");
                    t = src[ip++];
                    if (t >= 16)
                    {
                        state = 2;
                        continue;
                    }
                    if (ip >= srcLen) throw Corrupt("input overrun");
                    mPos = op - (1 + m2MaxOffset);
                    mPos -= t >> 2;
                    mPos -= src[ip++] << 2;
                    if (mPos < 0) throw Corrupt("lookbehind overrun");
                    if (op + 3 > dstLen) throw Corrupt("output overrun");
                    dst[op++] = dst[mPos++];
                    dst[op++] = dst[mPos++];
                    dst[op++] = dst[mPos];
                    // match_done
                    t = src[ip - 2] & 3;
                    if (t == 0)
                    {
                        state = 0;
                        continue;
                    }
                    state = 3;
                    continue;
                }

                if (state == 3)
                {
                    // match_next: copy 1..3 literals then read next instruction
                    if (ip + t + 1 > srcLen) throw Corrupt("input overrun");
                    if (op + t > dstLen) throw Corrupt("output overrun");
                    dst[op++] = src[ip++];
                    if (t > 1)
                    {
                        dst[op++] = src[ip++];
                        if (t > 2)
                            dst[op++] = src[ip++];
                    }
                    t = src[ip++];
                    state = 2;
                    continue;
                }

                // state == 2: match
                if (t >= 64)
                {
                    // M2
                    if (ip >= srcLen) throw Corrupt("input overrun");
                    mPos = op - 1;
                    if (lzo1y)
                    {
                        mPos -= (t >> 2) & 3;
                        mPos -= src[ip++] << 2;
                        t = (t >> 4) - 3;
                    }
                    else
                    {
                        mPos -= (t >> 2) & 7;
                        mPos -= src[ip++] << 3;
                        t = (t >> 5) - 1;
                    }
                }
                else if (t >= 32)
                {
                    // M3
                    t &= 31;
                    if (t == 0)
                    {
                        while (true)
                        {
                            if (ip >= srcLen) throw Corrupt("input overrun");
                            if (src[ip] != 0) break;
                            t += 255;
                            ip++;
                        }
                        t += 31 + src[ip++];
                    }
                    if (ip + 2 > srcLen) throw Corrupt("input overrun");
                    mPos = op - 1;
                    mPos -= (src[ip] >> 2) + (src[ip + 1] << 6);
                    ip += 2;
                }
                else if (t >= 16)
                {
                    // M4
                    mPos = op;
                    mPos -= (t & 8) << 11;
                    t &= 7;
                    if (t == 0)
                    {
                        while (true)
                        {
                            if (ip >= srcLen) throw Corrupt("input overrun");
                            if (src[ip] != 0) break;
                            t += 255;
                            ip++;
                        }
                        t += 7 + src[ip++];
                    }
                    if (ip + 2 > srcLen) throw Corrupt("input overrun");
                    mPos -= (src[ip] >> 2) + (src[ip + 1] << 6);
                    ip += 2;
                    if (mPos == op)
                        return op; // end of stream marker
                    mPos -= 0x4000;
                }
                else
                {
                    // M1: 2 byte match
                    if (ip >= srcLen) throw Corrupt("input overrun");
                    mPos = op - 1;
                    mPos -= t >> 2;
                    mPos -= src[ip++] << 2;
                    if (mPos < 0) throw Corrupt("lookbehind overrun");
                    if (op + 2 > dstLen) throw Corrupt("output overrun");
                    dst[op++] = dst[mPos++];
                    dst[op++] = dst[mPos];
                    t = src[ip - 2] & 3;
                    state = t == 0 ? 0 : 3;
                    continue;
                }

                // copy_match: t + 2 bytes, may overlap so copy byte by byte
                int len = t + 2;
                if (mPos < 0) throw Corrupt("lookbehind overrun");
                if (op + len > dstLen) throw Corrupt("output overrun");
                for (int i = 0; i < len; i++)
                    dst[op++] = dst[mPos++];

                // match_done
                t = src[ip - 2] & 3;
                state = t == 0 ? 0 : 3;
            }
        }
    }
}
