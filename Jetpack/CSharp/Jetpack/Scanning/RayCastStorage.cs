using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jetpack.Scanning
{
    public class RayCastStorage
    {
        public enum RayCategory
        {
            ConfinedArea_Ico,
            RepelGround,
            ObstacleAvoidance_BodyEllipse,
        }

        public class RayCastBundle
        {
            public RayInfo[] Rays { get; set; }
        }

        public class RayInfo
        {
            public Vector3 Origin { get; set; }
            public Vector3 Direction { get; set; }
            public float MaxLen { get; set; }

            public RaycastHit? Hit { get; set; }     // will be null if there is no hit
        }

        private Dictionary<RayCategory, RayCastBundle> _storage = new Dictionary<RayCategory, RayCastBundle>();

        // called at the beginning of an update
        public void Clear()
        {
            _storage.Clear();
        }

        // enum of category, list of raycasts and corresponding hits
        public void AddRayCasts(RayCategory category, RayCastBundle bundle)
        {
            if (_storage.ContainsKey(category))
                _storage[category] = bundle;
            else
                _storage.Add(category, bundle);
        }

        /// <summary>
        /// Returns requested bundles, or all if no categories passed in
        /// </summary>
        public RayCastBundle[] GetRayCasts(params RayCategory[] categories)
        {
            if (categories.Length == 0)
                return _storage.Values.ToArray();

            var retVal = new List<RayCastBundle>();

            foreach (RayCategory category in categories)
                if (_storage.TryGetValue(category, out RayCastBundle bundle))
                    retVal.Add(bundle);

            return retVal.ToArray();
        }
    }
}
