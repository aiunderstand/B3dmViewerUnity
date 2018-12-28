using B3dm.Tile;
using GLTF;
using GLTF.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityGLTF;
using Newtonsoft.Json;
using b3dm;

public class b3dmStreamLoader : MonoBehaviour
{
    //Notes on b3dm TRANSFORMS (see https://github.com/AnalyticalGraphicsInc/3d-tiles/tree/master/specification)

    //Master transform    
    //http://www.oc.nps.edu/oc2902w/coord/llhxyz.htm //convert ELEF coordinates to WGS84 lat, long, heigth;
    //example: 3887868.815, 332860.248, 5028369.523 -> Lat 52.18818 deg N, Lon 4.89345 deg E, Height 6358446269.4 meter

    //Child (of child of child ..) transform
    //Bounding volume (box). The boundingVolume.box property is an array of 12 numbers that define an oriented bounding box in a right-handed 3-axis (x, y, z).
    //Cartesian coordinate system where the z-axis is up.
    //The first three elements define the x, y, and z values for the center of the box. 
    //The next three elements (with indices 3, 4, and 5) define the x-axis direction and half-length.
    //The next three elements(indices 6, 7, and 8) define the y-axis direction and half-length.
    //The last three elements(indices 9, 10, and 11) define the z-axis direction and half-length.
    //example root boundingbox 0: 0 1: 0 2: 0 3: 1740.124 4: 0 5: 0 6: 0 7: 2378.07 8: 0 9: 0 10: 0 11: 1256.995 
    //0, 0, 0 Center
    //1740.124, 0, 0 x as -> half length so Axis = 2* value
    //0, 2378.07 y as -> half length so Axis = 2* value
    //0,0, 1256.995 z as -> half length so Axis = 2* value

    //example child[0] 0: 1.139 1: -167.195 2: 0 3: 1737.846 4: 0 5: 0 6: 0 7: 2043.679 8: 0 9: 0 10: 0 11: 1256.995 
    //1.139, -167.195, 0 Center
    //1737.846, 0, 0 x as -> half length so Axis = 2* value
    //0, 2043.679 y as -> half length so Axis = 2* value
    //0,0, 1256.995 z as -> half length so Axis = 2* value

    private void Awake()
    {
        //get and parse tile.json 
        string url = @"https://saturnus.geodan.nl/tomt/data/buildingtiles_adam/tileset.json";
        StartCoroutine(DownloadTilesetJson(url));
    }

    private IEnumerator DownloadTilesetJson(string url)
    {
        DownloadHandlerBuffer handler = new DownloadHandlerBuffer();
        var www = new UnityWebRequest(url)
        {
            downloadHandler = handler
        };
        yield return www.SendWebRequest();

        if (!www.isNetworkError && !www.isHttpError)
        {
            //get data
            string jsonString = www.downloadHandler.text;

            //convert to jsonTree
            var json = JsonConvert.DeserializeObject<b3dm.Rootobject>(jsonString);

            //collect all url nodes in jsonTree and add to list
            List<string> tiles = new List<string>();

            foreach (var c in json.root.children)
            {
                tiles.Add(c.content.url);

                if (c.children.Length > 0)
                    AddToTiles(c.children, tiles);
            }

            //download and load tiles
            StartCoroutine(ServerIsToSlowHack(tiles));
        }
        else
        {
            Debug.Log("Tile: [" + url + "] Error loading tileset data");
        }
    }

    private IEnumerator ServerIsToSlowHack(List<string> tiles)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            StartCoroutine(DownloadB3DMTile(tiles[i]));
            yield return null;
        }
    }

    private void AddToTiles(Child[] children, List<string> tiles)
    {
        foreach (var c in children)
        {
            tiles.Add(c.content.url);

            if (c.children.Length > 0)
                AddToTiles(c.children, tiles);
        }
           
    }

    private IEnumerator DownloadB3DMTile(string url)
    {
        DownloadHandlerBuffer handler = new DownloadHandlerBuffer();
        var www = new UnityWebRequest(@"https://saturnus.geodan.nl/tomt/data/buildingtiles_adam/" +url)
        {
            downloadHandler = handler
        };
        yield return www.SendWebRequest();

        if (!www.isNetworkError && !www.isHttpError)
        {
            //get data
            var stream = new MemoryStream(www.downloadHandler.data);

            var b3dm = B3dmParser.ParseB3dm(stream);

            var memoryStream = new MemoryStream(b3dm.GlbData);
            Load(memoryStream);
        }
        else
        {
            Debug.Log("Tile: [" + url +"] Error loading b3dm data");
        }
    }

    private void Load(Stream stream)
    {
        GLTFRoot gLTFRoot;
        GLTFParser.ParseJson(stream, out gLTFRoot);
        var loader = new GLTFSceneImporter(gLTFRoot, null, null, stream);
        loader.LoadSceneAsync();
    }

}
