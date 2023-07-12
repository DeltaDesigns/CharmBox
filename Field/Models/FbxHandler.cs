using System.Runtime.InteropServices;
using Field.Entities;
using Field.General;
using Field.Statics;
using Internal.Fbx;
using System.Numerics;
using System.Collections.Concurrent;

namespace Field.Models;


public class FbxHandler
{
    private FbxManager _manager;
    private FbxScene _scene;
    public InfoConfigHandler InfoHandler;
    List<TagHash> addedEntities = new List<TagHash>();
    private static object _fbxLock = new object();
    public List<FbxNode> _globalSkeletonNodes = new List<FbxNode>();  // used for attaching all models to one skeleton
    public List<BoneNode> _globalBoneNodes = new List<BoneNode>();
    public FbxHandler(bool bMakeInfoHandler=true)
    {
        lock (_fbxLock) // bc fbx is not thread-safe
        {
            _manager = FbxManager.Create();
            _scene = FbxScene.Create(_manager, "");
        }

        if (bMakeInfoHandler)
            InfoHandler = new InfoConfigHandler();
    }

    public FbxMesh AddMeshPartToScene(Part part, int index, string meshName)
    {
        FbxMesh mesh = CreateMeshPart(part, index, meshName);
        FbxNode node;
        lock (_fbxLock)
        {
            node = FbxNode.Create(_manager, mesh.GetName());
        }
        node.SetNodeAttribute(mesh);
        
        if (part.VertexNormals.Count > 0)
        {
            AddNormalsToMesh(mesh, part);
        }
        
        if (part.VertexTangents.Count > 0)
        {
            AddTangentsToMesh(mesh, part);
        }

        if (part.VertexTexcoords.Count > 0)
        {
            AddTexcoordsToMesh(mesh, part);
        }

        if (part.VertexColours.Count > 0)
        {
            AddColoursToMesh(mesh, part);
        }
        
        if (part.VertexColourSlots.Count > 0 || part.GearDyeChangeColorIndex != 0xFF)  // api item, so do slots and uv1
        {
            AddSlotColoursToMesh(mesh, part);
            AddTexcoords1ToMesh(mesh, part);
        }

        // for importing to other engines
        if (InfoHandler != null && part.Material != null) // todo consider why some materials are null
        {
            InfoHandler.AddMaterial(part.Material);
            InfoHandler.AddPart(part, node.GetName());   
        }


        AddMaterial(mesh, node, index, part.Material);
        AddSmoothing(mesh);
        
        lock (_fbxLock)
        {
            //node.LclRotation.Set(new FbxDouble3(-90, 0, 0)); //This is fucking up the source 2 map import for whatever reason and I cant be bothered to suffer through that again
            _scene.GetRootNode().AddChild(node);
        }
        
        return mesh;
    }

    private FbxMesh CreateMeshPart(Part part, int index, string meshName)
    {
        bool done = false;
        FbxMesh mesh;
        lock (_fbxLock)
        {
            mesh = FbxMesh.Create(_manager, $"{meshName}_Group{part.GroupIndex}_Index{part.Index}_{index}_{part.LodCategory}");
        }

        // Conversion lookup table
        Dictionary<int, int> lookup = new Dictionary<int, int>();
        for (int i = 0; i < part.VertexIndices.Count; i++)
        {
            lookup[(int)part.VertexIndices[i]] = i;
        }
        foreach (int vertexIndex in part.VertexIndices)
        {
            // todo utilise dictionary to make this control point thing better maybe?
            var pos = part.VertexPositions[lookup[vertexIndex]];
            mesh.SetControlPointAt(new FbxVector4(pos.X, pos.Y, pos.Z, 1), lookup[vertexIndex]);
        }
        foreach (var face in part.Indices)
        {
            mesh.BeginPolygon();
            mesh.AddPolygon(lookup[(int)face.X]);
            mesh.AddPolygon(lookup[(int)face.Y]);
            mesh.AddPolygon(lookup[(int)face.Z]);
            mesh.EndPolygon();
        }

        mesh.CreateLayer();
        return mesh;
    }

    private void AddNormalsToMesh(FbxMesh mesh, Part part)
    {
        FbxLayerElementNormal normalsLayer;
        lock (_fbxLock)
        {
            normalsLayer = FbxLayerElementNormal.Create(mesh, "normalLayerName");
        }
        normalsLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        normalsLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        // Check if quaternion
        foreach (var normal in part.VertexNormals)
        {
            normalsLayer.GetDirectArray().Add(new FbxVector4(normal.X, normal.Y, normal.Z));
        }
        mesh.GetLayer(0).SetNormals(normalsLayer);
    }
    
    private void AddTangentsToMesh(FbxMesh mesh, Part part)
    {
        FbxLayerElementTangent tangentsLayer;
        lock (_fbxLock)
        {
            tangentsLayer = FbxLayerElementTangent.Create(mesh, "tangentLayerName");
        }
        tangentsLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        tangentsLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        // todo more efficient to do AddMultiple
        foreach (var tangent in part.VertexTangents)
        {
            tangentsLayer.GetDirectArray().Add(new FbxVector4(tangent.X, tangent.Y, tangent.Z));
        }
        mesh.GetLayer(0).SetTangents(tangentsLayer);
    }

    
    private void AddTexcoordsToMesh(FbxMesh mesh, Part part)
    {
        FbxLayerElementUV uvLayer;
        lock (_fbxLock)
        {
            uvLayer = FbxLayerElementUV.Create(mesh, "uv0");
        }
        uvLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        uvLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        foreach (var tx in part.VertexTexcoords)
        {
            uvLayer.GetDirectArray().Add(new FbxVector2(tx.X, tx.Y));
        }
        mesh.GetLayer(0).SetUVs(uvLayer);
    }
    
    private void AddTexcoords1ToMesh(FbxMesh mesh, Part part)
    {
        FbxLayerElementUV uvLayer;
        lock (_fbxLock)
        {
            uvLayer = FbxLayerElementUV.Create(mesh, "uv1");
        }
        uvLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        uvLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        foreach (var tx in part.VertexTexcoords1)
        {
            uvLayer.GetDirectArray().Add(new FbxVector2(tx.X, tx.Y));
        }
        if (mesh.GetLayer(1) == null)
            mesh.CreateLayer();
        mesh.GetLayer(1).SetUVs(uvLayer);
    }

    
    private void AddColoursToMesh(FbxMesh mesh, Part part)
    {
        FbxLayerElementVertexColor colLayer;
        lock (_fbxLock)
        {
            colLayer = FbxLayerElementVertexColor.Create(mesh, "colourLayerName");
        }
        colLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        colLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        foreach (var colour in part.VertexColours)
        {
            colLayer.GetDirectArray().Add(new FbxColor(colour.X, colour.Y, colour.Z, colour.W));
        }
        mesh.GetLayer(0).SetVertexColors(colLayer);
    }

    private void AddSlotColoursToMesh(FbxMesh mesh, Part part)
    {
        FbxLayerElementVertexColor colLayer;
        lock (_fbxLock)
        {
            colLayer = FbxLayerElementVertexColor.Create(mesh, "slots");
        }
        colLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        colLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        if (part.PrimitiveType == EPrimitiveType.Triangles)
        {
            VertexBuffer.AddSlotInfo(part, part.GearDyeChangeColorIndex);
            for (var i = 0; i < part.VertexPositions.Count; i++)
            {
                colLayer.GetDirectArray().Add(new FbxColor(part.VertexColourSlots[0].X, part.VertexColourSlots[0].Y, part.VertexColourSlots[0].Z, part.VertexColourSlots[0].W));
            }
        }
        else
        {
            foreach (var colour in part.VertexColourSlots)
            {
                colLayer.GetDirectArray().Add(new FbxColor(colour.X, colour.Y, colour.Z, colour.W));
            }
        }

        if (mesh.GetLayer(1) == null)
            mesh.CreateLayer();
        mesh.GetLayer(1).SetVertexColors(colLayer);
    }

    /// <summary>
    /// Bind pose uses global transforms?
    /// </summary>
    private void AddBindPose(List<FbxNode> clusterNodes, List<BoneNode> boneNodes)
    {
        FbxPose pose = FbxPose.Create(_scene, "bindPoseName");
        pose.SetIsBindPose(true);
        
        for (int i = 0; i < clusterNodes.Count; i++)
        {
            // Setting the global transform for each cluster (but really its node link)
            var node = clusterNodes[i];
            var boneNode = boneNodes[i];
            // setting the bind matrix from DOST
            FbxMatrix bindMatrix = new FbxMatrix();
            bindMatrix.SetIdentity();
            bindMatrix.SetTQS(
                boneNode.DefaultObjectSpaceTransform.Translation.ToFbxVector4(),
                boneNode.DefaultObjectSpaceTransform.QuaternionRotation.ToFbxQuaternion(),
                new FbxVector4(boneNode.DefaultObjectSpaceTransform.Scale, boneNode.DefaultObjectSpaceTransform.Scale, boneNode.DefaultObjectSpaceTransform.Scale)
            );
            pose.Add(node, bindMatrix);
        }
        
        _scene.AddPose(pose);
    }

    private void AddWeightsToMesh(FbxMesh mesh, DynamicPart part, List<FbxNode> skeletonNodes)
    {
        FbxSkin skin;
        lock (_fbxLock)
        {
            skin = FbxSkin.Create(_manager, "skinName");
        }
        HashSet<int> seen = new HashSet<int>();
        
        List<FbxCluster> weightClusters = new List<FbxCluster>();
        foreach (var node in skeletonNodes)
        {
            FbxCluster weightCluster;
            lock (_fbxLock)
            {
                weightCluster = FbxCluster.Create(_manager, node.GetName());
            }
            weightCluster.SetLink(node);
            weightCluster.SetLinkMode(FbxCluster.ELinkMode.eTotalOne);
            FbxAMatrix transform = node.EvaluateGlobalTransform(); // dodgy?
            weightCluster.SetTransformLinkMatrix(transform);
            
            
            
            weightClusters.Add(weightCluster);
        }
        
        // Conversion lookup table
        Dictionary<int, int> lookup = new Dictionary<int, int>();
        for (int i = 0; i < part.VertexIndices.Count; i++)
        {
            lookup[(int)part.VertexIndices[i]] = i;
        }
        foreach (int v in part.VertexIndices)
        {
            VertexWeight vw = part.VertexWeights[lookup[v]];
            for (int j = 0; j < 4; j++)
            {
                if (vw.WeightValues[j] != 0)
                {
                    if (vw.WeightIndices[j] < weightClusters.Count)
                    {
                        seen.Add(vw.WeightIndices[j]);
                        weightClusters[vw.WeightIndices[j]].AddControlPointIndex(lookup[v], (float)vw.WeightValues[j]/255);
                    }
                }
            }
        }
        
        foreach (var c in weightClusters)
        {
            skin.AddCluster(c);
        }
        
        mesh.AddDeformer(skin);
    }
    
    private FbxAMatrix GetGeometry(FbxNode pNode)
    {
        FbxVector4 lT = pNode.GetGeometricTranslation(FbxNode.EPivotSet.eSourcePivot);
        FbxVector4 lR = pNode.GetGeometricRotation(FbxNode.EPivotSet.eSourcePivot);
        FbxVector4 lS = pNode.GetGeometricScaling(FbxNode.EPivotSet.eSourcePivot);

        return new FbxAMatrix(lT, lR, lS);
    }

    private void AddMaterial(FbxMesh mesh, FbxNode node, int index, Material material)
    {
        if (material == null)
            return;
        
        FbxSurfacePhong fbxMaterial;
        FbxLayerElementMaterial materialLayer;
        lock (_fbxLock)
        {
            fbxMaterial = FbxSurfacePhong.Create(_scene, material.Hash);
            materialLayer = FbxLayerElementMaterial.Create(mesh, $"matlayer_{node.GetName()}_{index}");
        }
        fbxMaterial.DiffuseFactor.Set(1);
        node.SetShadingMode(FbxNode.EShadingMode.eTextureShading);
        node.AddMaterial(fbxMaterial);

        // if this doesnt exist, it wont load the material slots in unreal
        materialLayer.SetMappingMode(FbxLayerElement.EMappingMode.eAllSame);
        mesh.GetLayer(0).SetMaterials(materialLayer);
    }

    private void AddSmoothing(FbxMesh mesh)
    {
        FbxLayerElementSmoothing smoothingLayer;
        lock (_fbxLock)
        {
            smoothingLayer = FbxLayerElementSmoothing.Create(mesh, $"smoothingLayerName");
        }
        smoothingLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByEdge);
        smoothingLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);

        FbxArrayInt edges = mesh.mEdgeArray;
        List<int> sharpEdges = new List<int>();
        var numEdges = edges.GetCount();
        for (int i = 0; i < numEdges; i++)
        {
            smoothingLayer.GetDirectArray().Add(i);
        }
        
        mesh.GetLayer(0).SetSmoothing(smoothingLayer);
        
        mesh.SetMeshSmoothness(FbxMesh.ESmoothness.eFine);
    }

    public void AddPlayerSkeletonAndMesh(bool skipModel = false)
    {
        // player skeleton + necklace and hands
        Entity playerBase = PackageHandler.GetTag(typeof(Entity), new TagHash("0000670F342E9595")); // 64 bit more permanent 
        AddEntityToScene(playerBase, playerBase.Load(ELOD.MostDetail), ELOD.MostDetail);

        if (!skipModel)
        {
            uint helm = Convert.ToUInt32(FieldConfigHandler.GetAnimationHelmetHash());
            uint arms = Convert.ToUInt32(FieldConfigHandler.GetAnimationArmsHash());
            uint chest = Convert.ToUInt32(FieldConfigHandler.GetAnimationChestHash());
            uint legs = Convert.ToUInt32(FieldConfigHandler.GetAnimationLegsHash());
            uint classitem = Convert.ToUInt32(FieldConfigHandler.GetAnimationClassItemHash());
            List<uint> models = new List<uint>
            {
                helm,
                chest,
                arms,
                legs,
                classitem
            };
            foreach (var model in models)
            {
                var entities = InvestmentHandler.GetEntitiesFromHash(new DestinyHash(model));
                var entity = entities[0];
                var parts = entity.Load(ELOD.MostDetail);
                AddEntityToScene(entity, parts, ELOD.MostDetail);
            }
        }
    }

    public List<FbxNode> AddSkeleton(List<BoneNode> boneNodes)
    {
        FbxNode rootNode = null;
        List<FbxNode> skeletonNodes = new List<FbxNode>();
        foreach (var boneNode in boneNodes)
        {
            FbxSkeleton skeleton;
            FbxNode node;
            lock (_fbxLock)
            {
                skeleton = FbxSkeleton.Create(_manager, boneNode.Hash.ToString());
                node = FbxNode.Create(_manager, boneNode.Hash.ToString());
            }
            skeleton.SetSkeletonType(FbxSkeleton.EType.eLimbNode);
            node.SetNodeAttribute(skeleton);

            Vector3 location = boneNode.DefaultObjectSpaceTransform.Translation;
            Vector4 rotation = boneNode.DefaultObjectSpaceTransform.QuaternionRotation;
      
            if (boneNode.ParentNodeIndex != -1)
            {
                rotation -= boneNodes[boneNode.ParentNodeIndex].DefaultObjectSpaceTransform.QuaternionRotation;
                location -= boneNodes[boneNode.ParentNodeIndex].DefaultObjectSpaceTransform.Translation;
            }

            //Console.WriteLine($"{boneNode.Hash}: {rotation.X}, {rotation.Y}, {rotation.Z}, {rotation.W}");
            //FbxQuaternion quat = new();
            //quat.Set(rotation.X, rotation.Y, rotation.Z, rotation.W);

            //FbxVector4 eular = new();
            //eular.SetXYZ(quat);

            //node.LclRotation.Set(eular.ToDouble3());
            node.LclTranslation.Set(new FbxDouble3(location.X, location.Y, location.Z));

            if (rootNode == null)
            {
                skeleton.SetSkeletonType(FbxSkeleton.EType.eRoot);
                rootNode = node;
            }
            else
            {
                skeletonNodes[boneNode.ParentNodeIndex].AddChild(node);
            }
            skeletonNodes.Add(node);
        }

        _scene.GetRootNode().AddChild(rootNode);
        return skeletonNodes;
    }
    
    public List<FbxNode> MakeFbxSkeletonHierarchy(List<BoneNode> boneNodes)
    {
        var jointNodes = new List<FbxNode>();

        for (int i = 0; i < boneNodes.Count; i++)
        {
            var node = boneNodes[i];
            FbxNode parentNode;

            FbxSkeleton skeleton;
            FbxNode joint;
            lock (_fbxLock)
            {
                skeleton = FbxSkeleton.Create(_manager, node.Hash.ToString());
                joint = FbxNode.Create(_manager, node.Hash.ToString());
            }
            skeleton.SetSkeletonType(FbxSkeleton.EType.eLimbNode);
            joint.SetNodeAttribute(skeleton);
            jointNodes.Add(joint);

            if (node.ParentNodeIndex >= 0)
            {
                parentNode = jointNodes[node.ParentNodeIndex];
                parentNode.AddChild(joint);
            }
            else
            {
                lock (_fbxLock)
                {
                    FbxSkeleton rootNodeSkeleton;
                    FbxNode rootNode;
                    rootNodeSkeleton = FbxSkeleton.Create(_manager, node.Hash.ToString());
                    rootNode = FbxNode.Create(_manager, node.Hash.ToString());
                    rootNode.AddChild(joint);
                    rootNode.SetNodeAttribute(rootNodeSkeleton);
                    _scene.GetRootNode().AddChild(rootNode);
                }
            }

            // Set the transform
            FbxAMatrix globalTransform = joint.EvaluateGlobalTransform();
            FbxAMatrix objectSpaceTransform = new FbxAMatrix();
            objectSpaceTransform.SetIdentity();
            objectSpaceTransform.SetT(node.DefaultObjectSpaceTransform.Translation.ToFbxVector4());
            objectSpaceTransform.SetQ(node.DefaultObjectSpaceTransform.QuaternionRotation.ToFbxQuaternion());

            FbxAMatrix localTransform = globalTransform.Inverse().mul(objectSpaceTransform);
            var localTranslation = localTransform.GetT();
            var localRotation = localTransform.GetR();
            joint.LclTranslation.Set(localTranslation.ToDouble3());
            joint.LclRotation.Set(localRotation.ToDouble3());
        }
        return jointNodes;
    }


    public void ExportScene(string fileName)
    {
        // Make directory for file
        string directory = Path.GetDirectoryName(fileName);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        lock (_fbxLock)
        {
            if (_manager.GetIOSettings() == null)
            {
                FbxIOSettings ios = FbxIOSettings.Create(_manager, FbxWrapperNative.IOSROOT);
                _manager.SetIOSettings(ios);
            }
            _manager.GetIOSettings().SetBoolProp(FbxWrapperNative.EXP_FBX_MATERIAL, true);
            _manager.GetIOSettings().SetBoolProp(FbxWrapperNative.EXP_FBX_TEXTURE, true);
            _manager.GetIOSettings().SetBoolProp(FbxWrapperNative.EXP_FBX_EMBEDDED, true);
            _manager.GetIOSettings().SetBoolProp(FbxWrapperNative.EXP_FBX_ANIMATION, true);
            _manager.GetIOSettings().SetBoolProp(FbxWrapperNative.EXP_FBX_GLOBAL_SETTINGS, true);
            var exporter = FbxExporter.Create(_manager, "");
            exporter.Initialize(fileName, -1);  // -1 == use binary not ascii, binary is more space efficient
            exporter.Export(_scene);
            exporter.Destroy();
        }
        _scene.Clear();
        if (InfoHandler != null)
            InfoHandler.WriteToFile(directory);

    }

    public void AddEntityToScene(Entity entity, List<DynamicPart> dynamicParts, ELOD detailLevel, Animation animation=null, List<FbxNode> skeletonNodes = null, bool skipBlanks = false)
    {
        if (skeletonNodes == null)
        {
            skeletonNodes = new List<FbxNode>();
        }
        // _scene.GetRootNode().LclRotation.Set(new FbxDouble3(90, 0, 0));
        // List<FbxNode> skeletonNodes = new List<FbxNode>();
        
        // List<FbxNode> skeletonNodes = new List<FbxNode>();
        if (entity.Skeleton != null)
        {
            // skeletonNodes = AddSkeleton(entity.Skeleton.GetBoneNodes());
            _globalBoneNodes = entity.Skeleton.GetBoneNodes();
            _globalSkeletonNodes = MakeFbxSkeletonHierarchy(_globalBoneNodes);
        }
        for( int i = 0; i < dynamicParts.Count; i++)
        {
            var dynamicPart = dynamicParts[i];

            if (dynamicPart.Material == null)
                continue;

            if (skipBlanks)
            {
                if (dynamicPart.Material.Header.PSTextures.Count == 0) //Dont know if this will 100% "fix" the duplicate meshs that come with entities
                {
                    continue;
                }
            }

            FbxMesh mesh = AddMeshPartToScene(dynamicPart, i, entity.Hash);
            
            if (dynamicPart.VertexWeights.Count > 0)
            {
                // if (skeletonNodes.Count > 0)
                // {
                //     AddWeightsToMesh(mesh, dynamicPart, skeletonNodes);
                //     AddBindPose(skeletonNodes, entity.Skeleton.GetBoneNodes());
                // }
                if (_globalSkeletonNodes.Count > 0)
                {
                    AddWeightsToMesh(mesh, dynamicPart, _globalSkeletonNodes);
                    AddBindPose(_globalSkeletonNodes, _globalBoneNodes);
                }
            }
        }
        if (animation != null)
            AddAnimationToEntity(animation, _globalSkeletonNodes, entity);
    }


    public void AddStaticToScene(List<Part> parts, string meshName)
    {
        for( int i = 0; i < parts.Count; i++)
        {
            Part part = parts[i];
            AddMeshPartToScene(part, i, meshName);
        }
    }
    
    public void AddAnimationToEntity(Animation animation, List<FbxNode> skeletonNodes, Entity entity = null)
    {
        string entHash = "";
        if (entity != null) //scuffed
            entHash = $"{entity.Hash}_";

        animation.Load();
        
        FbxAnimStack animStack;
        FbxAnimLayer animLayer;
        FbxTime time;
        lock (_fbxLock)
        {
            animStack = FbxAnimStack.Create(_scene, $"{entHash}animStack_{animation.Hash}");
            animLayer = FbxAnimLayer.Create(_scene, $"");
            time = new FbxTime();
            animStack.AddMember(animLayer);        
        }
        string[] dims = { "X", "Y", "Z" };
        
        foreach (var track in animation.Tracks)
        {
            var scale = dims.Select(x => skeletonNodes[track.TrackIndex].LclScaling.GetCurve(animLayer, x, true)).ToList();
            var rotation = dims.Select(x => skeletonNodes[track.TrackIndex].LclRotation.GetCurve(animLayer, x, true)).ToList();
            var translation = dims.Select(x => skeletonNodes[track.TrackIndex].LclTranslation.GetCurve(animLayer, x, true)).ToList();

            scale.ForEach(x => x.KeyModifyBegin());
            rotation.ForEach(x => x.KeyModifyBegin());
            translation.ForEach(x => x.KeyModifyBegin());

            for (int d = 0; d < dims.Length; d++)
            {
                for (int i = 0; i < track.TrackTimes.Count; i++)
                {
                    float frameTime = track.TrackTimes[i];
                    time.SetSecondDouble(frameTime);

                    if (track.TrackScales.Count > 0)
                    {
                        var scaleKeyIndex = scale[d].KeyAdd(time);
                        scale[d].KeySetValue(scaleKeyIndex, track.TrackScales[i]);
                        scale[d].KeySetInterpolation(scaleKeyIndex, FbxAnimCurveDef.EInterpolationType.eInterpolationLinear);
                    }

                    if (track.TrackRotations.Count > 0)
                    {
                        var rotDim = Array.FindIndex(dims, x => x == animation.rotXYZ[d]);
                        var rotationKeyIndex = rotation[d].KeyAdd(time);
                        rotation[d].KeySetValue(rotationKeyIndex, (animation.flipRot[d] == 1 ? -1 : 1) * track.TrackRotations[i][rotDim] + animation.rot[d]);
                        rotation[d].KeySetInterpolation(rotationKeyIndex, FbxAnimCurveDef.EInterpolationType.eInterpolationLinear);
                    }

                    if (track.TrackTranslations.Count > 0)
                    {
                        var traDim = Array.FindIndex(dims, x => x == animation.traXYZ[d]);
                        var translationKeyIndex = translation[d].KeyAdd(time);
                        translation[d].KeySetValue(translationKeyIndex, (animation.flipTra[d] == 1 ? -1 : 1) * track.TrackTranslations[i][traDim] + animation.tra[d]);
                        translation[d].KeySetInterpolation(translationKeyIndex, FbxAnimCurveDef.EInterpolationType.eInterpolationLinear);
                    }
                } 
            }

            scale.ForEach(x => x.KeyModifyEnd());
            rotation.ForEach(x => x.KeyModifyEnd());
            translation.ForEach(x => x.KeyModifyEnd());     
        }
    }

    public void AddAnimationToEntity(Animation animation)
    {
        AddAnimationToEntity(animation, _globalSkeletonNodes);
    }

    public void AddAnimationsToEntity(ConcurrentDictionary<Animation, int> animations, ConcurrentDictionary<int, int> timings, List<FbxNode> skeletonNodes, Entity entity = null)
    {
        string entHash = "";
        if (entity != null) //scuffed
            entHash = $"{entity.Hash}_";

        double currentFrameIndex = 0;
        FbxAnimStack animStack;
        FbxAnimLayer animLayer;
        FbxTime time;

        lock (_fbxLock)
        {
            animStack = FbxAnimStack.Create(_scene, $"{entHash}animStack");
            animLayer = FbxAnimLayer.Create(_scene, $"");
            time = new FbxTime();
            animStack.AddMember(animLayer);
        }

        //Console.WriteLine(animations.Count);
        foreach (var data in animations.OrderBy(x => x.Value))
        {
            var animation = data.Key;
            int order = data.Value;
            int suborder = 0;
            var startFrame = timings[order];
            animation.Load();

            //Console.WriteLine($"{order} : {startFrame}");
            string[] dims = { "X", "Y", "Z" };
            
            foreach (var track in animation.Tracks)
            {
                var scale = dims.Select(x => skeletonNodes[track.TrackIndex].LclScaling.GetCurve(animLayer, x, true)).ToList();
                var rotation = dims.Select(x => skeletonNodes[track.TrackIndex].LclRotation.GetCurve(animLayer, x, true)).ToList();
                var translation = dims.Select(x => skeletonNodes[track.TrackIndex].LclTranslation.GetCurve(animLayer, x, true)).ToList();

                scale.ForEach(x => x.KeyModifyBegin());
                rotation.ForEach(x => x.KeyModifyBegin());
                translation.ForEach(x => x.KeyModifyBegin());

                for (int d = 0; d < dims.Length; d++)
                {
                    for (int i = 0; i < track.TrackTimes.Count; i++)
                    {
                        float frameTime = track.TrackTimes[i];
                        //time.SetSecondDouble(frameTime + currentFrameIndex);
                        time.SetFramePrecise(startFrame + i, FbxTime.EMode.eFrames30);

                        if (track.TrackScales.Count > 0)
                        {   
                            var scaleKeyIndex = scale[d].KeyAdd(time);
                            scale[d].KeySetValue(scaleKeyIndex, track.TrackScales[i]);
                            scale[d].KeySetInterpolation(scaleKeyIndex, i == track.TrackTimes.Count - 1 ? FbxAnimCurveDef.EInterpolationType.eInterpolationConstant : FbxAnimCurveDef.EInterpolationType.eInterpolationLinear);
                        }

                        if (track.TrackRotations.Count > 0)
                        {
                            var rotDim = Array.FindIndex(dims, x => x == animation.rotXYZ[d]);
                            var rotationKeyIndex = rotation[d].KeyAdd(time);
                            rotation[d].KeySetValue(rotationKeyIndex, (animation.flipRot[d] == 1 ? -1 : 1) * track.TrackRotations[i][rotDim] + animation.rot[d]);
                            rotation[d].KeySetInterpolation(rotationKeyIndex, i == track.TrackTimes.Count - 1 ? FbxAnimCurveDef.EInterpolationType.eInterpolationConstant : FbxAnimCurveDef.EInterpolationType.eInterpolationLinear);
                        }

                        if (track.TrackTranslations.Count > 0)
                        {
                            var traDim = Array.FindIndex(dims, x => x == animation.traXYZ[d]);
                            var translationKeyIndex = translation[d].KeyAdd(time);
                            translation[d].KeySetValue(translationKeyIndex, (animation.flipTra[d] == 1 ? -1 : 1) * track.TrackTranslations[i][traDim] + animation.tra[d]);
                            translation[d].KeySetInterpolation(translationKeyIndex, i == track.TrackTimes.Count - 1 ? FbxAnimCurveDef.EInterpolationType.eInterpolationConstant : FbxAnimCurveDef.EInterpolationType.eInterpolationLinear);
                        }
                    }
                }

                scale.ForEach(x => x.KeyModifyEnd());
                rotation.ForEach(x => x.KeyModifyEnd());
                translation.ForEach(x => x.KeyModifyEnd());
            }

            //float animationTotalFrames = animation.Header.FrameCount;
            double animationTotalFrames = animation.Tracks.SelectMany(t => t.TrackTimes).Max();
            currentFrameIndex += animationTotalFrames;
        }
    }

    public void AddAnimationsToEntity(ConcurrentDictionary<Animation, int> animations, ConcurrentDictionary<int, int> timings)
    {
        AddAnimationsToEntity(animations, timings, _globalSkeletonNodes);
    }

    public void Clear()
    {
        _scene.Clear();
        _globalSkeletonNodes.Clear();
        _globalBoneNodes.Clear();
    }

    public void Dispose()
    {
        lock (_fbxLock)
        {
            _scene.Destroy();
            _manager.Destroy();
        }
        if(InfoHandler != null)
            InfoHandler.Dispose();
    }

    public void AddStaticInstancesToScene(List<Part> parts, List<D2Class_406D8080> instances, string meshName)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            FbxMesh mesh = CreateMeshPart(parts[i], i, meshName);
            for (int j = 0; j < instances.Count; j++)
            {
                FbxNode node;
                lock (_fbxLock)
                {
                    node = FbxNode.Create(_manager, $"{meshName}_{i}_{j}");
                }
                node.SetNodeAttribute(mesh);
                Vector4 quatRot = new Vector4(instances[j].Rotation.X, instances[j].Rotation.Y, instances[j].Rotation.Z, instances[j].Rotation.W);
                Vector3 eulerRot = quatRot.QuaternionToEulerAnglesZYX();
                
                node.LclTranslation.Set(new FbxDouble3(instances[j].Position.X, instances[j].Position.Y, instances[j].Position.Z));
                node.LclRotation.Set(new FbxDouble3(eulerRot.X, eulerRot.Y, eulerRot.Z));
                node.LclScaling.Set(new FbxDouble3(instances[j].Scale.X, instances[j].Scale.X, instances[j].Scale.X));
                
                lock (_fbxLock)
                {
                    _scene.GetRootNode().AddChild(node);
                }
            }
        }
    }

    public void AddDynamicToScene(D2Class_85988080 points, TagHash entityHash, string savePath, bool bSaveShaders = false, bool bSaveCBuffers = false, bool skipCheck = false)
    {
        Entity entity = PackageHandler.GetTag(typeof(Entity), entityHash);

        if(!skipCheck)
            if (!entity.HasGeometry())
            {
                return;
            }
     
        if (InfoHandler != null)
            InfoHandler.AddInstance(entity.Hash, points.Translation.W, points.Rotation, points.Translation.ToVec3());

        if (!addedEntities.Contains(entity.Hash))
        {
            addedEntities.Add(entity.Hash);
            //Console.WriteLine($"Added {entity.Hash}");
            List<FbxNode> skeletonNodes = new List<FbxNode>();
            List<DynamicPart> dynamicParts = entity.Load(ELOD.MostDetail, true);
            entity.SaveMaterialsFromParts(savePath, dynamicParts, bSaveShaders, bSaveCBuffers);
            
            if (entity.Skeleton != null)
            {
                skeletonNodes = AddSkeleton(entity.Skeleton.GetBoneNodes());
            }
            for (int i = 0; i < dynamicParts.Count; i++)
            {
                var dynamicPart = dynamicParts[i];

                if (dynamicPart.Material == null)
                    continue;

                if (dynamicPart.Material.Header.PSTextures.Count == 0) //Dont know if this will 100% "fix" the duplicate meshs that come with entities
                {
                    continue;
                }

                FbxMesh mesh = AddMeshPartToScene(dynamicPart, i, entity.Hash);

                if (dynamicPart.VertexWeights.Count > 0)
                {
                    if (skeletonNodes.Count > 0)
                    {
                        AddWeightsToMesh(mesh, dynamicPart, skeletonNodes);
                    }
                }
            }
        }
    }

    public void AddEmptyToScene(string emptyName, Vector4 position, Vector4 rotation)
    {
        FbxNode node;
        lock (_fbxLock)
        {
            node = FbxNode.Create(_manager, $"{emptyName}");
        }

        Vector3 eulerRot = rotation.QuaternionToEulerAnglesZYX();

        node.LclTranslation.Set(new FbxDouble3(position.X * 100, position.Z * 100, -position.Y * 100));
        node.LclRotation.Set(new FbxDouble3(eulerRot.X, eulerRot.Y, eulerRot.Z));
        node.LclScaling.Set(new FbxDouble3(100, 100, 100));

        lock (_fbxLock)
        {
            _scene.GetRootNode().AddChild(node);
        }
    }

    public void AddCameraToScene(string camName)
    {
        FbxCamera camera = FbxCamera.Create(_manager, $"Camera_{camName}");
        camera.SetAspect(FbxCamera.EAspectRatioMode.eFixedResolution, 1920, 800);
        camera.SetFarPlane(100000);
        camera.SetNearPlane(1);

        FbxNode cameraNode = FbxNode.Create(_manager, $"CameraNode_{camName}");
        cameraNode.SetNodeAttribute(camera);

        FbxNode parentNode = _globalSkeletonNodes[1];

        FbxAMatrix parentTransform = parentNode.EvaluateGlobalTransform();
        FbxDouble3 pTranslation = parentTransform.GetT().ToDouble3();
        FbxDouble3 pRotation = parentTransform.GetR().ToDouble3();
        FbxDouble3 pScale = parentTransform.GetS().ToDouble3();
        cameraNode.SetNodeAttribute(camera);
        cameraNode.LclTranslation.Set(pTranslation);
        cameraNode.LclRotation.Set(pRotation);
        cameraNode.LclScaling.Set(pScale);

        parentNode.AddChild(cameraNode);
        _scene.GetGlobalSettings().SetDefaultCamera($"Camera_{camName}");
    }


    // From https://github.com/OwlGamingCommunity/V/blob/492d0cb3e89a97112ac39bf88de39da57a3a1fbf/Source/owl_core/Server/MapLoader.cs
    private static System.Numerics.Vector3 QuaternionToEulerAngles(Quaternion q)
    {
        System.Numerics.Vector3 retVal = new System.Numerics.Vector3();

        // roll (x-axis rotation)
        double sinr_cosp = +2.0 * (q.W * q.X + q.Y * q.Z);
        double cosr_cosp = +1.0 - 2.0 * (q.X * q.X + q.Y * q.Y);
        retVal.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

        // pitch (y-axis rotation)
        double sinp = +2.0 * (q.W * q.Y - q.Z * q.X);
        double absSinP = Math.Abs(sinp);
        bool bSinPOutOfRage = absSinP >= 1.0;
        if (bSinPOutOfRage)
        {
            retVal.Y = 90.0f; // use 90 degrees if out of range
        }
        else
        {
            retVal.Y = (float)Math.Asin(sinp);
        }

        // yaw (z-axis rotation)
        double siny_cosp = +2.0 * (q.W * q.Z + q.X * q.Y);
        double cosy_cosp = +1.0 - 2.0 * (q.Y * q.Y + q.Z * q.Z);
        retVal.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

        // Rad to Deg
        retVal.X *= (float)(180.0f / Math.PI);

        if (!bSinPOutOfRage) // only mult if within range
        {
            retVal.Y *= (float)(180.0f / Math.PI);
        }
        retVal.Z *= (float)(180.0f / Math.PI);

        return retVal;
    }

    public void SetGlobalSkeleton(TagHash tagHash)
    {
        EntitySkeleton skeleton = PackageHandler.GetTag(typeof(EntitySkeleton), tagHash);
        _globalBoneNodes = skeleton.GetBoneNodes();
        _globalSkeletonNodes = MakeFbxSkeletonHierarchy(_globalBoneNodes);
    }
}