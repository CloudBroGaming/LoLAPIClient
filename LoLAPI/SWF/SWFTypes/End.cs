using System.IO;

namespace CloudBroGaming.LoLAPI.SWF.SWFTypes
{
    public class End : Tag
    {
        public End()
        {
            this.TagCode = (int)TagCodes.End;
        }

        public override void ReadData(byte version, BinaryReader binaryReader)
        {
            RecordHeader rh = new RecordHeader();
            rh.ReadData(binaryReader);
        }
    }
}