using System.Collections.Concurrent;
using System.Text.Json;
using Field.Models;
using Field.Statics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Field.General;

public class InfoConfigHandler
{
    public bool bOpen = false;
    private ConcurrentDictionary<string, dynamic> _config = new ConcurrentDictionary<string, dynamic>();

    public InfoConfigHandler()
    {
        ConcurrentDictionary<string, Dictionary<string, Dictionary<int, TexInfo>>> mats = new ConcurrentDictionary<string, Dictionary<string, Dictionary<int, TexInfo>>>();
        _config.TryAdd("Materials", mats);
        ConcurrentDictionary<string, string> parts = new ConcurrentDictionary<string, string>();
        _config.TryAdd("Parts", parts);
        ConcurrentDictionary<string, ConcurrentBag<JsonInstance>> instances = new ConcurrentDictionary<string, ConcurrentBag<JsonInstance>>();
        _config.TryAdd("Instances", instances);
        
        //ConcurrentDictionary<string, ConcurrentBag<JsonSound>> sounds = new ConcurrentDictionary<string, ConcurrentBag<JsonSound>>();
        //_config.TryAdd("Sounds", sounds);

        ConcurrentDictionary<string, ConcurrentBag<JsonCubemap>> cubemaps = new ConcurrentDictionary<string, ConcurrentBag<JsonCubemap>>();
        _config.TryAdd("Cubemaps", cubemaps);

        ConcurrentDictionary<string, ConcurrentBag<JsonDecal>> decals = new ConcurrentDictionary<string, ConcurrentBag<JsonDecal>>();
        _config.TryAdd("Decals", decals);

        ConcurrentDictionary<string, ConcurrentBag<JsonLight>> lights = new ConcurrentDictionary<string, ConcurrentBag<JsonLight>>();
        _config.TryAdd("Lights", lights);

        bOpen = true;
    }

    public void Dispose()
    {
        if (bOpen)
        {
            _config.Clear();
            bOpen = false; 
        }
    }

    public void AddMaterial(Material material)
    {
        if (!material.Hash.IsValid())
        {
            return;
        }
        Dictionary<string, Dictionary<int, TexInfo>> textures = new Dictionary<string, Dictionary<int, TexInfo>>();
        if (!_config["Materials"].TryAdd(material.Hash, textures))
        {
            return;
        }
        Dictionary<int, TexInfo> vstex = new Dictionary<int, TexInfo>();
        textures.Add("VS", vstex);
        foreach (var vst in material.Header.VSTextures)
        {
            if (vst.Texture != null)
            {
                vstex.Add((int)vst.TextureIndex, new TexInfo {Hash = vst.Texture.Hash, SRGB = vst.Texture.IsSrgb() });
            }
        }
        Dictionary<int, TexInfo> pstex = new Dictionary<int, TexInfo>();
        textures.Add("PS", pstex);
        foreach (var pst in material.Header.PSTextures)
        {
            if (pst.Texture != null)
            {
                pstex.Add((int)pst.TextureIndex, new TexInfo {Hash = pst.Texture.Hash, SRGB = pst.Texture.IsSrgb() });
            }
        }
    }
    
    public void AddPart(Part part, string partName)
    {
        _config["Parts"].TryAdd(partName, part.Material.Hash.GetHashString());
    }

    public void AddType(string type)
    {
        _config["Type"] = type;
    }

    public void SetMeshName(string meshName)
    {
        _config["MeshName"] = meshName;
    }

    public void SetUnrealInteropPath(string interopPath)
    {
        _config["UnrealInteropPath"] = new string(interopPath.Split("\\Content").Last().ToArray()).TrimStart('\\');
        if (_config["UnrealInteropPath"] == "")
        {
            _config["UnrealInteropPath"] = "Content";
        }
    }

    public void AddInstance(string modelHash, float scale, Vector4 quatRotation, Vector3 translation)
    {
        if (!_config["Instances"].ContainsKey(modelHash))
        {
            _config["Instances"][modelHash] = new ConcurrentBag<JsonInstance>();
        }
        _config["Instances"][modelHash].Add(new JsonInstance
        {
            Translation = new [] { translation.X, translation.Y, translation.Z },
            Rotation = new [] { quatRotation.X, quatRotation.Y, quatRotation.Z, quatRotation.W },
            Scale = scale
        });
    }

    public void AddCubemap(string name, Vector3 scale, Vector4 quatRotation, Vector3 translation)
    {
        if (!_config["Cubemaps"].ContainsKey(name))
        {
            _config["Cubemaps"][name] = new ConcurrentBag<JsonCubemap>();
        }
        _config["Cubemaps"][name].Add(new JsonCubemap
        {
            Translation = new[] { translation.X, translation.Y, translation.Z },
            Rotation = new[] { quatRotation.X, quatRotation.Y, quatRotation.Z, quatRotation.W },
            Scale = new[] { scale.X, scale.Y, scale.Z }
        });
    }

    public void AddDecal(string boxhash, string materialName, Vector4 origin, Vector4 corner1, Vector4 corner2)
    {
        if (!_config["Decals"].ContainsKey(boxhash))
        {
            _config["Decals"][boxhash] = new ConcurrentBag<JsonDecal>();
        }
        _config["Decals"][boxhash].Add(new JsonDecal
        {
            Material = materialName,
            Origin = new[] { origin.X, origin.Y, origin.Z },
            Scale = origin.W,
            Corner1 = new[] { corner1.X, corner1.Y, corner1.Z },
            Corner2 = new[] { corner2.X, corner2.Y, corner2.Z }
        });
    }

    public void AddStaticInstances(List<D2Class_406D8080> instances, string staticMesh)
    {
        foreach (var instance in instances)
        {
            AddInstance(staticMesh, instance.Scale.X, instance.Rotation, instance.Position);
        }
    }

    public void AddLight(string name, string type, Vector4 translation, Vector4 quatRotation, Vector2 size, Vector4 color)
    {
        //Idfk how color/intensity is handled, so if its above 1 then bring it down
        var R = color.X > 1 ? color.X / 100 : color.X;
        var G = color.Y > 1 ? color.Y / 100 : color.Y;
        var B = color.Z > 1 ? color.Z / 100 : color.Z;

        if (!_config["Lights"].ContainsKey(name))
        {
            _config["Lights"][name] = new ConcurrentBag<JsonLight>();
        }
        _config["Lights"][name].Add(new JsonLight
        {
            Type = type,
            Translation = new[] { translation.X, translation.Y, translation.Z },
            Rotation = new[] { quatRotation.X, quatRotation.Y, quatRotation.Z, quatRotation.W },
            Size = new[] { size.X, size.Y },
            Color = new[] { R, G, B }
        });
    }

    public void AddSound(string soundHash, float range, Vector4 translation)
    {
        if (!_config["Sounds"].ContainsKey(soundHash))
        {
            _config["Sounds"][soundHash] = new ConcurrentBag<JsonSound>();
        }
        _config["Sounds"][soundHash].Add(new JsonSound
        {
            Translation = new[] { translation.X, translation.Y, translation.Z },
            Range = range
        });
    }

    public void AddSounds(D2Class_6D668080 audioContainer)
    {
        Console.WriteLine($"Added {audioContainer.AudioContainer.Hash} to cfg");
        var b = PackageHandler.GetTag<D2Class_38978080>(audioContainer.AudioContainer.Hash);
        foreach (var sound in b.Header.Unk20)
        {
            AddSound(sound.Hash, audioContainer.Unk40, audioContainer.AudioPositions[0].Translation);
        }
    }

    public void AddCustomTexture(string material, int index, TextureHeader texture)
    {
        if (!_config["Materials"].ContainsKey(material))
        {
            var textures = new Dictionary<string, Dictionary<int, TexInfo>>();
            textures.Add("PS",  new Dictionary<int, TexInfo>());
            _config["Materials"][material] = textures;
        }
        _config["Materials"][material]["PS"].TryAdd(index, new TexInfo { Hash = texture.Hash, SRGB = texture.IsSrgb()});
    }
    
    public void WriteToFile(string path)
    {

        // If theres only 1 part, we need to rename it + the instance to the name of the mesh (unreal imports to fbx name if only 1 mesh inside)
        if (_config["Parts"].Count == 1)
        {
            var part = _config["Parts"][_config["Parts"].Keys[0]];
            //I'm not sure what to do if it's 0, so I guess I'll leave that to fix it in the future if something breakes.
            if (_config["Instances"].Count != 0)
            {
                var instance = _config["Instances"][_config["Instances"].Keys[0]];
                _config["Instances"] = new ConcurrentDictionary<string, ConcurrentBag<JsonInstance>>();
                _config["Instances"][_config["MeshName"]] = instance;
            }
            _config["Parts"] = new ConcurrentDictionary<string, string>();
            _config["Parts"][_config["MeshName"]] = part;
        }

        
        //im not smart enough to have done this, so i made an ai do it lol
        //this just sorts the "instances" part of the cfg so its ordered by scale
        //makes it easier for instancing models in Hammer/S&Box

        var sortedDict = new ConcurrentDictionary<string, ConcurrentBag<JsonInstance>>();

        // Use LINQ's OrderBy method to sort the values in each array
        // based on the "Scale" key. The lambda expression specifies that
        // the "Scale" property should be used as the key for the order.
        foreach (var keyValuePair in (ConcurrentDictionary<string, ConcurrentBag<JsonInstance>>)_config["Instances"])
        {
            var array = keyValuePair.Value;
            var sortedArray = array.OrderBy(x => x.Scale);

            // Convert the sorted array to a ConcurrentBag
            var sortedBag = new ConcurrentBag<JsonInstance>(sortedArray);

            // Add the sorted bag to the dictionary
            sortedDict.TryAdd(keyValuePair.Key, sortedBag);
        }

        // Finally, update the _config["Instances"] object with the sorted values
        _config["Instances"] = sortedDict;

        
        string s = JsonConvert.SerializeObject(_config, Formatting.Indented);
        if (_config.ContainsKey("MeshName"))
        {
            File.WriteAllText($"{path}/{_config["MeshName"]}_info.cfg", s);
        }
        else
        {
            File.WriteAllText($"{path}/info.cfg", s);
        }
        Dispose();
    }

    private struct JsonInstance
    {
        public float[] Translation;
        public float[] Rotation;
        public float Scale;
    }
    private struct JsonSound
    {
        public float[] Translation;
        public float Range;
    }
    private struct JsonCubemap
    {
        public float[] Translation;
        public float[] Rotation;
        public float[] Scale;
    }
    private struct JsonDecal
    {
        public string Material;
        public float[] Origin;
        public float Scale;
        public float[] Corner1;
        public float[] Corner2;
    }
    private struct JsonLight
    {
        public string Type;
        public float[] Translation;
        public float[] Rotation;
        public float[] Size;
        public float[] Color;
    }
}

public struct TexInfo
{
    public string Hash  { get; set; }
    public bool SRGB  { get; set; }
}
