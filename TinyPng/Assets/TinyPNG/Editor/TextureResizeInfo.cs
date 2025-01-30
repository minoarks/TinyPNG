using Sirenix.OdinInspector;

namespace ARK.EditorTools.TinyPNG
{
    public class TextureResizeInfo
    {

        public string texture;
        
        public int  OriginKbSize;
        
        public int  compressedSize;

        [ProgressBar(0, "OriginKbSize")]
        public int compressedResult;

    }
}