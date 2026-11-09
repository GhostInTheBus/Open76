using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts.Entities
{
    // A stationary gun emplacement (mission objects such as mg1turr, c3turr, vxturr1). The mission places it like a
    // car, its .vcf names a model (vxturret.vdf, no wheels) and one weapon, but it never moves.
    //
    // Hit points come from the .vcf: each side has an armor value and a chassis value. A hit drains the armor of the
    // side that was struck, then the chassis of that side; when both are gone the turret is destroyed. This is an
    // approximation, the original damage rules for turrets are not documented.
    //
    // Turrets do not aim or fire yet: the data files carry no range, arc or tracking values for them (the weapon
    // .gdf fields are identical to the vehicle weapons), so that behavior is left out instead of invented.
    public class Turret : WorldEntity
    {
        private const int Sides = 4; // front, right, back, left
        private int[] _armor;
        private int[] _chassis;
        private int _startTotal;
        private int _healthGroups;
        private Transform _thirdPerson;

        public Vdf Vdf { get; private set; }
        public Vcf Vcf { get; private set; }
        public int TeamId { get; set; }
        public bool Attacked { get; private set; }

        public override bool Alive
        {
            get { return Total() > 0; }
        }

        public void Configure(Vdf vdf, Vcf vcf, Transform thirdPerson)
        {
            Vdf = vdf;
            Vcf = vcf;
            _thirdPerson = thirdPerson;
            _healthGroups = vdf.PartsThirdPerson.Count;
            _armor = new[] { (int)vcf.ArmorFront, (int)vcf.ArmorRight, (int)vcf.ArmorRear, (int)vcf.ArmorLeft };
            _chassis = new[] { (int)vcf.ChassisFront, (int)vcf.ChassisRight, (int)vcf.ChassisRear, (int)vcf.ChassisLeft };
            _startTotal = Total();
        }

        private int Total()
        {
            int total = 0;
            for (int i = 0; i < Sides; ++i)
            {
                total += _armor[i] + _chassis[i];
            }

            return total;
        }

        public override void ApplyDamage(DamageType damageType, Vector3 hitNormal, int damage)
        {
            if (!Alive)
            {
                return;
            }

            Attacked = true;

            // Same side selection as Car.ApplyDamage.
            float angle = Quaternion.FromToRotation(Vector3.up, hitNormal).eulerAngles.z;
            int side;
            if (angle >= 45f && angle < 135f) side = 1;
            else if (angle >= 135f && angle < 225f) side = 2;
            else if (angle >= 225f && angle < 315f) side = 3;
            else side = 0;

            int fromArmor = Mathf.Min(_armor[side], damage);
            _armor[side] -= fromArmor;
            _chassis[side] = Mathf.Max(0, _chassis[side] - (damage - fromArmor));

            SetHealthGroup();
        }

        // Show the model for the damage state: group 0 is undamaged, the last group is the wreck.
        private void SetHealthGroup()
        {
            if (_thirdPerson == null || _healthGroups <= 1)
            {
                return;
            }

            int total = Total();
            int group;
            if (total <= 0)
            {
                group = _healthGroups - 1;
            }
            else
            {
                int steps = _healthGroups - 1;
                group = steps - Mathf.CeilToInt(total / (float)_startTotal * steps);
            }

            for (int i = 0; i < _healthGroups; ++i)
            {
                Transform child = _thirdPerson.Find("Health " + i);
                if (child != null)
                {
                    child.gameObject.SetActive(i == group);
                }
            }
        }
    }
}
