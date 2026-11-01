using UnityEngine;

namespace BlueLike.Setup
{
    /// <summary>
    /// Procedural placeholder art. Everything here exists so you can play the game before a
    /// single model is imported, and so you have something to compare the toon shader against.
    /// </summary>
    public static class PrimitiveFactory
    {
        private static Shader _toonShader;
        private static Shader _hairShader;

        public static Shader ToonShader
        {
            get
            {
                if (_toonShader == null)
                {
                    _toonShader = Shader.Find("BlueLike/AnimeToon");
                    if (_toonShader == null) _toonShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (_toonShader == null) _toonShader = Shader.Find("Standard");
                }
                return _toonShader;
            }
        }

        public static Shader HairShader
        {
            get
            {
                if (_hairShader == null)
                {
                    _hairShader = Shader.Find("BlueLike/AnimeToonHair");
                    if (_hairShader == null) _hairShader = ToonShader;
                }
                return _hairShader;
            }
        }

        public static Material MakeToonMaterial(Color baseColor, float outlineWidth = 1.2f)
        {
            var mat = new Material(ToonShader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", outlineWidth);
            if (mat.HasProperty("_ShadowColor"))
                mat.SetColor("_ShadowColor", Color.Lerp(baseColor, new Color(0.40f, 0.42f, 0.62f), 0.55f));
            return mat;
        }

        /// <summary>
        /// Builds a stand-in character: body capsule, head sphere, and the halo ring that
        /// makes the silhouette read the way you want it to.
        /// </summary>
        public static GameObject MakePlaceholderCharacter(string label, Color bodyColor, Color haloColor, out Renderer bodyRenderer, out Transform muzzle)
        {
            var root = new GameObject(label + "_Model");

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            body.transform.localScale = new Vector3(0.52f, 0.62f, 0.52f);
            Object.Destroy(body.GetComponent<Collider>());
            bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.sharedMaterial = MakeToonMaterial(bodyColor);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            head.transform.localScale = Vector3.one * 0.48f;
            Object.Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().sharedMaterial =
                MakeToonMaterial(new Color(0.98f, 0.90f, 0.84f));

            // Halo: a real torus so it reads as a ring rather than a disc.
            var halo = new GameObject("Halo");
            halo.transform.SetParent(root.transform, false);
            halo.transform.localPosition = new Vector3(0f, 2.18f, 0f);
            halo.transform.localRotation = Quaternion.Euler(74f, 0f, 12f);
            var haloFilter = halo.AddComponent<MeshFilter>();
            haloFilter.sharedMesh = BuildTorus(0.26f, 0.028f, 20, 8);
            var haloRenderer = halo.AddComponent<MeshRenderer>();
            Material haloMat = MakeToonMaterial(haloColor, 0f);
            if (haloMat.HasProperty("_EmissionColor")) haloMat.SetColor("_EmissionColor", haloColor * 2f);
            if (haloMat.HasProperty("_OutlineWidth")) haloMat.SetFloat("_OutlineWidth", 0f);
            haloRenderer.sharedMaterial = haloMat;
            haloRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            halo.AddComponent<HaloSpin>();

            // Muzzle anchor. When you swap in a real model, point this at the weapon tip.
            var muzzleGo = new GameObject("Muzzle");
            muzzleGo.transform.SetParent(root.transform, false);
            muzzleGo.transform.localPosition = new Vector3(0.18f, 1.15f, 0.42f);
            muzzle = muzzleGo.transform;

            return root;
        }

        /// <summary>Generates a torus mesh. Used for the halo; handy for rings and auras too.</summary>
        public static Mesh BuildTorus(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
        {
            majorSegments = Mathf.Max(3, majorSegments);
            minorSegments = Mathf.Max(3, minorSegments);

            int vertCount = (majorSegments + 1) * (minorSegments + 1);
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            for (int i = 0; i <= majorSegments; i++)
            {
                float u = (float)i / majorSegments;
                float theta = u * Mathf.PI * 2f;
                Vector3 centre = new Vector3(Mathf.Cos(theta) * majorRadius, 0f, Mathf.Sin(theta) * majorRadius);
                Vector3 outward = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));

                for (int j = 0; j <= minorSegments; j++)
                {
                    float v = (float)j / minorSegments;
                    float phi = v * Mathf.PI * 2f;
                    Vector3 normal = outward * Mathf.Cos(phi) + Vector3.up * Mathf.Sin(phi);

                    int index = i * (minorSegments + 1) + j;
                    verts[index] = centre + normal * minorRadius;
                    normals[index] = normal;
                    uvs[index] = new Vector2(u, v);
                }
            }

            var tris = new int[majorSegments * minorSegments * 6];
            int t = 0;
            for (int i = 0; i < majorSegments; i++)
            {
                for (int j = 0; j < minorSegments; j++)
                {
                    int a = i * (minorSegments + 1) + j;
                    int b = a + minorSegments + 1;

                    tris[t++] = a;
                    tris[t++] = b;
                    tris[t++] = a + 1;

                    tris[t++] = a + 1;
                    tris[t++] = b;
                    tris[t++] = b + 1;
                }
            }

            var mesh = new Mesh { name = "Torus" };
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
