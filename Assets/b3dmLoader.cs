using B3dm.Tile;
using GLTF;
using GLTF.Schema;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;

public class b3dmLoader : MonoBehaviour
{
    private void Awake()
    {
        string path = Application.dataPath + "/1311.b3dm";
        var fileStream = File.OpenRead(path);
        var b3dm = B3dmParser.ParseB3dm(fileStream);

        var memoryStream = new MemoryStream(b3dm.GlbData);
        Load(memoryStream);
    }

    private void Load(Stream stream)
    {
            GLTFRoot gLTFRoot;
            GLTFParser.ParseJson(stream, out gLTFRoot);
            var loader = new GLTFSceneImporter(gLTFRoot, null, null, stream);
            loader.LoadSceneAsync();
    }
}
