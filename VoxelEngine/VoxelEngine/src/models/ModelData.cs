namespace VoxelEngine.src.models;

public struct ModelData
{
    public List<ModelElement> Elements { get; set; }

    public struct ModelElement
    {
        public float[] From { get; set; }
        public float[] To { get; set; }
        public ElementRotation? Rotation { get; set; }
        public Dictionary<BlockFace, ElementFace> Faces { get; set; }


        public struct ElementRotation
        {
            public float[] Origin { get; set; }
            public string Axis { get; set; }
            public int Angle { get; set; }
            public bool Rescale { get; set; }
        }
        public struct ElementFace
        {
            public float[] UVs { get; set; }
            public int Rotation { get; set; }
            public string Texture { get; set; }
        }
    }
}

public struct StateData
{
    public ModelData Model { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
