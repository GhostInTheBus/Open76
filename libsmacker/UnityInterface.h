#pragma once

#include "smacker.h"
#include <stdint.h>

// Export macro: MSVC on Windows, default visibility on macOS/Linux (clang/gcc).
#if defined(_WIN32)
#define SMK_EXPORT __declspec(dllexport)
#else
#define SMK_EXPORT __attribute__((visibility("default")))
#endif

const int MaxAudioTracks = 7;

struct SmkAudioData
{
	unsigned char TrackMask;
	unsigned char AudioChannels[MaxAudioTracks];
	uint32_t AudioRate[MaxAudioTracks];
	unsigned char BitDepth[MaxAudioTracks];
	uint32_t AudioDataSize[MaxAudioTracks];
	unsigned char* AudioData[MaxAudioTracks];
};

extern "C"
{
	// Open an SMK file from disk - return video width, height, frame count and microseconds between frames.
	SMK_EXPORT void* OpenFile(const char* filePath, int& width, int& height, int& frameCount, float& microSecondsPerFrame);

	// Clean up SMK file.
	SMK_EXPORT void DisposeFile(smk smkFileHandle);

	// Fill Color32 buffer with current frame's data.
	SMK_EXPORT void GetFrameData(smk smkFileHandle, unsigned char* buffer, unsigned int bufferSize);

	// Attempt to advance to the next frame, returns false if there are no more frames.
	SMK_EXPORT bool AdvanceFrame(smk smkFileHandle);
	
	// Load raw audio data.
	SMK_EXPORT void* GetAudioData(smk smkFileHandle);
	
	// Clean up audio data.
	SMK_EXPORT void DisposeAudioData(SmkAudioData* audioData);
}