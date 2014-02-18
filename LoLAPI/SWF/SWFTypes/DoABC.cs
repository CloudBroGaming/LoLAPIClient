﻿using System;
using System.IO;

namespace CloudBroGaming.LoLAPI.SWF.SWFTypes
{
    public class DoABC : Tag
    {
        private byte[] actionRecord;
        public byte[] ABCData;
        public string Name;
        public UInt32 Flags;

        public DoABC()
        {
            TagCode = (int)TagCodes.DoABC;
        }

        public DoABC(byte[] actionRecord)
        {
            this.actionRecord = actionRecord;
            TagCode = (int)TagCodes.DoABC;
        }

        public override int ActionRecCount
        {
            get
            {
                return 1;
            }
        }

        public override byte[] this[int index]
        {
            get
            {
                return actionRecord;
            }
            set
            {
                actionRecord = value;
            }
        }

        public override void ReadData(byte version, BinaryReader binaryReader)
        {
            RecordHeader rh = new RecordHeader();
            rh.ReadData(binaryReader);

            int length = System.Convert.ToInt32(rh.TagLength);
            actionRecord = binaryReader.ReadBytes(length);

            //This doesn't read correctly but it reads as good as we need
            using (BinaryReader b = new BinaryReader(new MemoryStream(actionRecord)))
            {
                Flags = b.ReadUInt32();
                Name = b.ReadString();
                ABCData = b.ReadBytes((int)(b.BaseStream.Length - b.BaseStream.Position)); //Might wrap around
            }
        }
    }
}