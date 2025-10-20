using ThunderRoad;
using UnityEngine;

namespace Jetpack2.Models
{
    public class TestConfigData : CustomData        // this CustomData inherits CatalogData
    {
        public static int testInt;     // looks like it's fields, not props
        public int TestInt { get => testInt; set => testInt = value; }      // this instance property appears to be necessary, otherwise the config file is ignored

        public override void Init()
        {
            base.Init();

            Debug.Log($"ConfigData init.  testInt: {testInt}");
        }

        // call this if the values were changed in game and want to be persisted
        public void SaveValues()
        {
            Catalog.SaveToJson(this);
        }

        private void Test()
        {
            // this looks like a helper class to work with CatalogData
            //Catalog.GetData


            //DebugViz
            //Viz.Lines().

        }
    }
}
