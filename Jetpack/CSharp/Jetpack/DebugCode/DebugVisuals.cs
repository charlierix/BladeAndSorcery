using PerfectlyNormalBaS;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.DebugCode
{
    public class DebugVisuals
    {
        private const bool SHOULD_DRAW = false;

        private DebugRenderer3D _renderer = null;

        public void AddVisuals()
        {
            if (!SHOULD_DRAW)
                return;

            if (_renderer == null)
            {
                Debug.Log("Wiring up DebugRenderer3D");
                _renderer = Player.local.gameObject.AddComponent<DebugRenderer3D>();
                Debug.Log("Wired up DebugRenderer3D");
            }

            // Creates a dot that stays where it's spawned
            //SpawnDot2();

            // Creates a dot that stays with transform.pos as the player flies around (on the floor, center of the irl room that player walks around)
            //SpawnDot3();

            // Uses debug renderer to spawn a static dot
            SpawnDot4();
        }

        private void SpawnDot()
        {
            Vector3 pos = Player.local.transform.position;
            Debug.Log($"Player.local.transform.position: {pos}");

            //pos = Player.local.transform.TransformPoint(pos);
            //Debug.Log($"Player.local.transform.TransformPoint: {pos}");

            Debug.Log($"Adding debug dot: {pos.ToStringSignificantDigits(2)}");
            //_renderer.AddDot(pos, 1f, UtilityUnity.ColorFromHex("FF0"));



            // This is invisible, but still darkens the area
            //  maybe the wrong shader?
            //  maybe needs a different layer?
            //
            // It would probably be better to follow a tutorial and make a simple weapon mod.  That might show what is going wrong



            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            //Collider collider = dot.GetComponent<Collider>();
            //if (collider != null)
            //    UnityEngine.Object.Destroy(collider);

            dot.transform.position = pos;
            dot.transform.localScale = new Vector3(9, 9, 9);

            MeshRenderer mesh = dot.GetComponent<MeshRenderer>();


            // This goes straight for the shader
            //mesh.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            // This creates a new instance of SimpleLit
            //Material simpleLitCopy = Instantiate(Resources.Load<Material>("Packages/Universal RP/Runtime/Materials/SimpleLit"));


            //mesh.material = Material.

            //mesh.material.color = Color.yellow;
            //mesh.material.SetColor("Base Map", Color.yellow);


            Debug.Log($"Added debug dot: {pos.ToStringSignificantDigits(2)}");
        }
        private void SpawnDot2()
        {
            Vector3 pos = Player.local.transform.position;
            Debug.Log($"Adding debug dot: {pos.ToStringSignificantDigits(2)}");

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            Collider collider = dot.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            dot.transform.position = pos;
            dot.transform.localScale = new Vector3(9, 9, 9);

            MeshRenderer mesh = dot.GetComponent<MeshRenderer>();

            // Can also use "ThunderRoad/LitMoss"
            // D:\blade and sorcery\BasSDK_2\Assets\SDK\Shaders\Lit_Moss.shader
            // https://kospy.github.io/BasSDK/Components/Guides/Shader/LitMoss.html

            mesh.material.shader = Shader.Find("Sprites/Default");      // BREAD — the default shader that's applied isn't in the game, need to change it

            //mesh.material.SetColor("_BaseColor", Color.yellow);     // doesn't do anything
            mesh.material.color = StaticRandom.ColorHSV();

            Debug.Log($"Added debug dot: {pos.ToStringSignificantDigits(2)}, {mesh.material.name}");
        }
        private void SpawnDot3()
        {
            // lyneca, bladey dancey man:
            // Call this in a loop to make a dot that moves to transform.position every frame
            // ThunderRoad.DebugViz.Viz.Dot(this, Player.local.transform.position).Color(Color.blue);

            Debug.Log($"Adding VizDot");

            EffectData effect_data = Catalog.GetData<EffectData>("PerfNormBeastJetpackVizDot2");
            EffectInstance instance = effect_data.Spawn(Player.local.transform);
            instance.Play();

            Debug.Log($"Added VizDot");
        }
        private void SpawnDot4()
        {
            Vector3 pos = Player.local.transform.position;
            Debug.Log($"Adding debug graphics: {pos.ToStringSignificantDigits(2)}");

            Vector3 offset_y = new Vector3(0, 3, 0);
            Vector3 offset_x = new Vector3(1, 0, 0);
            Vector3 offset_z = new Vector3(0, 0, 2);


            Color color = StaticRandom.ColorHSV();
            Color color_half = new Color(color.r, color.g, color.b, 0.5f);


            _renderer.AddDot(pos - offset_z, 1, color);
            //_renderer.AddDot(pos + offset_z, 1, color, true);     // lit shader can't be found (search for litmoss in DebugRenderer3D)
            _renderer.AddDot(pos + offset_z, 1, color_half);

            pos += offset_y;

            _renderer.AddCube(pos - offset_z, new Vector3(.25f, .5f, 1f), color);
            //_renderer.AddCube(pos + offset_z, new Vector3(.25f, .5f, 1f), color, true);
            _renderer.AddCube(pos + offset_z, new Vector3(.25f, .5f, 1f), color_half);

            pos += offset_y;

            _renderer.AddLine_Pipe(pos - offset_x - offset_z, pos + offset_x - offset_z, 0.05f, color);
            //_renderer.AddLine_Pipe(pos - offset_x + offset_z, pos + offset_x + offset_z, 0.05f, color, true);
            _renderer.AddLine_Pipe(pos - offset_x + offset_z, pos + offset_x + offset_z, 0.05f, color_half);

            pos += offset_y;

            _renderer.AddAxisLines(100, 1);

            _renderer.AddCircle(pos, Vector3.up, 1, 0.05f, color);

            pos += offset_y;

            _renderer.AddLine_Basic(pos - offset_x, pos + offset_x, 0.05f, color);

            pos += offset_y;

            _renderer.AddPlane_PointNormal(pos, Vector3.up, 1, color);

            Debug.Log($"Added debug added: {pos.ToStringSignificantDigits(2)}");
        }
    }
}