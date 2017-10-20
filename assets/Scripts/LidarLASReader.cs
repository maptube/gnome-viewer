using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

/// <summary>
/// Reader for LAS format lidar data
/// </summary>
public class LidarLASReader : MonoBehaviour {

    //editor properties
    [SerializeField]
    public string Filename;

    private LASPublicHeaderBlock Header;

    //struct of binary format
    public struct LASPublicHeaderBlock {
        public string FileSignature; //4 bytes "LASF"
        public UInt16 FileSourceID; // unsigned short 2 bytes*
        public UInt16 GlobalEncoding; // unsigned short 2 bytes* 
        public UInt32 ProjectID1; // - GUID data 1 unsigned long 4 bytes 
        public UInt16 ProjectID2; // - GUID data 2 unsigned short 2 byte 
        public UInt16 ProjectID3; // - GUID data 3 unsigned short 2 byte 
        public string ProjectID4; // - GUID data 4 unsigned char[8] 8 bytes 
        public byte VersionMajor; // unsigned char 1 byte* 
        public byte VersionMinor; // unsigned char 1 byte* 
        public string SystemIdentifier; // char[32] 32 bytes* 
        public string GeneratingSoftware; // char[32] 32 bytes* 
        public UInt16 FileCreationDayofYear; // unsigned short 2 bytes* 
        public UInt16 FileCreationYear; // unsigned short 2 bytes* 
        public UInt16 HeaderSize; // unsigned short 2 bytes* 
        public UInt32 OffsetToPointData; // unsigned long 4 bytes* 
        public UInt32 NumberofVariableLengthRecords; // unsigned long 4 bytes* 
        public byte PointDataRecordFormat; // unsigned char 1 byte* 
        public UInt16 PointDataRecordLength; // unsigned short 2 bytes* 
        public UInt32 LegacyNumberofPointRecords; // unsigned long 4 bytes* 
        public UInt32 [] LegacyNumberofPointsByReturn; // unsigned long[5] 20 bytes* 
        public double XScaleFactor; // double 8 bytes* 
        public double YScaleFactor; // double 8 bytes* 
        public double ZScaleFactor; // double 8 bytes* 
        public double XOffset; // double 8 bytes* 
        public double YOffset; // double 8 bytes* 
        public double ZOffset; // double 8 bytes* 
        public double MaxX; // double 8 bytes* 
        public double MinX; // double 8 bytes* 
        public double MaxY; // double 8 bytes* 
        public double MinY; // double 8 bytes* 
        public double MaxZ; // double 8 bytes* 
        public double MinZ; // double 8 bytes* 
        public UInt64 StartofWaveformDataPacketRecord; // Unsigned long long 8 bytes* 
        public UInt64 StartofFirstExtendedVariableLengthRecord; // unsigned long long 8 bytes* 
        public UInt32 NumberofExtendedVariableLengthRecords; // unsigned long 4 bytes* 
        public UInt64 NumberofPointRecords; // unsigned long long 8 bytes* 
        public UInt64[] NumberofPointsByReturn; // unsigned long long[15] 120 bytes*
    }

    //there are 10 point data record formats of varying data types and sizes!

    #region PointDataRecordFormats

    /// <summary>
    /// Base class for point 
    /// </summary>
    public class LASPointDataRecordFormat
    {
        public Int32 X; //4b
        public Int32 Y; //4b
        public Int32 Z; //4b
        public UInt16 Intensity; //2b
        public virtual void FromData(ref long pos, ref byte[] data)
        {
            this.X = ReadLong(ref pos, ref data);
            this.Y = ReadLong(ref pos, ref data);
            this.Z = ReadLong(ref pos, ref data);
        }
    }

    public class LASPointDataRecordFormat0 : LASPointDataRecordFormat
    {
        public byte ReturnNumberScanEdge; //1b
        public byte Classification; //1b
        public byte ScanAngleRank; //1b
        public byte UserData; //1b
        public UInt16 PointSourceID; //2b
        public override void FromData(ref long pos, ref byte [] data)
        {
            base.FromData(ref pos, ref data);
            this.Intensity = ReadUnsignedShort(ref pos, ref data);
            this.ReturnNumberScanEdge = ReadByte(ref pos, ref data);
            this.Classification = ReadByte(ref pos, ref data);
            this.ScanAngleRank = ReadByte(ref pos, ref data);
            this.UserData = ReadByte(ref pos, ref data);
            this.PointSourceID = ReadUnsignedShort(ref pos, ref data);
        }
    }

    /// <summary>
    /// Record format 1 is record format 0 with gps time added
    /// </summary>
    public class LASPointDataRecordFormat1 : LASPointDataRecordFormat0
    {
        public double GPSTime; //8b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos,ref data);
            this.GPSTime = ReadDouble(ref pos, ref data);
        }
    }

    /// <summary>
    /// Record format 2 is record format 0 with red green blue added
    /// </summary>
    public class LASPointDataRecordFormat2 : LASPointDataRecordFormat0
    {
        public UInt16 Red; //2b
        public UInt16 Green; //2b
        public UInt16 Blue; //2b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos, ref data);
            this.Red = ReadUnsignedShort(ref pos, ref data);
            this.Green = ReadUnsignedShort(ref pos, ref data);
            this.Blue = ReadUnsignedShort(ref pos, ref data);
        }
    }

    /// <summary>
    /// Record format 3 is Record format 2 with GPS time added, but it's before the RGB, so use record 1 and add RGB again
    /// </summary>
    public class LASPointDataRecordFormat3 : LASPointDataRecordFormat1
    {
        //GPS time comes before this
        public UInt16 Red; //2b
        public UInt16 Green; //2b
        public UInt16 Blue; //2b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos, ref data);
            this.Red = ReadUnsignedShort(ref pos, ref data);
            this.Green = ReadUnsignedShort(ref pos, ref data);
            this.Blue = ReadUnsignedShort(ref pos, ref data);
        }
    }

    /// <summary>
    /// Record 4 adds wave packets to record format 1
    /// </summary>
    public class LASPointDataRecordFormat4 : LASPointDataRecordFormat1
    {
        public byte WavePacketDescriptorIndex; //1b
        public UInt64 ByteOffsetToWaveformData; //8b
        public UInt32 WaveformPacketSizeInBytes; //4b
        public float ReturnPointWaveformLocation; //4b
        public float Xt; //4b
        public float Yt; //4b
        public float Zt; //4b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos, ref data);
            this.WavePacketDescriptorIndex = ReadByte(ref pos, ref data);
            this.ByteOffsetToWaveformData = ReadUnsignedLongLong(ref pos, ref data);
            this.WaveformPacketSizeInBytes = ReadUnsignedLong(ref pos, ref data);
            this.ReturnPointWaveformLocation = ReadUnsignedLong(ref pos, ref data);
            this.Xt = ReadFloat(ref pos, ref data);
            this.Yt = ReadFloat(ref pos, ref data);
            this.Zt = ReadFloat(ref pos, ref data);
        }
    }

    /// <summary>
    /// Format 5 adds wave packets to format 3
    /// </summary>
    public class LASPointDataRecordFormat5 : LASPointDataRecordFormat3
    {
        public byte WavePacketDescriptorIndex; //1b
        public UInt64 ByteOffsetToWaveformData; //8b
        public UInt32 WaveformPacketSizeInBytes; //4b
        public float ReturnPointWaveformLocation; //4b
        public float Xt; //4b
        public float Yt; //4b
        public float Zt; //4b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos, ref data);
            this.WavePacketDescriptorIndex = ReadByte(ref pos, ref data);
            this.ByteOffsetToWaveformData = ReadUnsignedLongLong(ref pos, ref data);
            this.WaveformPacketSizeInBytes = ReadUnsignedLong(ref pos, ref data);
            this.ReturnPointWaveformLocation = ReadUnsignedLong(ref pos, ref data);
            this.Xt = ReadFloat(ref pos, ref data);
            this.Yt = ReadFloat(ref pos, ref data);
            this.Zt = ReadFloat(ref pos, ref data);
        }
    }

    /// <summary>
    /// Format 6 contains more return numbers to support up to 15 returns so the bit size is increased.
    /// </summary>
    public class LASPointDataRecordFormat6 : LASPointDataRecordFormat
    {
        public byte ReturnNumberNumberOfReturns; //1b
        public byte ClassificationFlagsScannerChannelScanDirectionFlagEdgeOfFlightLine; //1b
        public byte Classification; //1b
        public byte UserData; //1b
        public Int16 ScanAngle; //2b
        public UInt16 PointSourceID; //2b
        public double GPSTime; //8b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos, ref data);
            this.ReturnNumberNumberOfReturns = ReadByte(ref pos, ref data);
            this.ClassificationFlagsScannerChannelScanDirectionFlagEdgeOfFlightLine = ReadByte(ref pos, ref data);
            this.Classification = ReadByte(ref pos, ref data);
            this.UserData = ReadByte(ref pos, ref data);
            this.ScanAngle = ReadShort(ref pos, ref data);
            this.PointSourceID = ReadUnsignedShort(ref pos, ref data);
            this.GPSTime = ReadDouble(ref pos, ref data);
        }
    }

    /// <summary>
    /// Format 7 adds red green blue to format 6
    /// </summary>
    public class LASPointDataRecordFormat7 : LASPointDataRecordFormat6
    {
        public UInt16 Red; //2b
        public UInt16 Green; //2b
        public UInt16 Blue; //2b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos, ref data);
            this.Red = ReadUnsignedShort(ref pos, ref data);
            this.Green = ReadUnsignedShort(ref pos, ref data);
            this.Blue = ReadUnsignedShort(ref pos, ref data);
        }
    }

    /// <summary>
    /// Format 8 adds NIR (near infrared) to format 7
    /// </summary>
    public class LASPointDataRecordFormat8 : LASPointDataRecordFormat7
    {
        public UInt16 NIR; //2b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos, ref data);
            this.NIR = ReadUnsignedShort(ref pos, ref data);
        }
    }

    /// <summary>
    /// Format 9 adds wave packets to format 6
    /// </summary>
    public class LASPointDataRecordFormat9 : LASPointDataRecordFormat6
    {
        public byte WavePacketDescriptorIndex; //1b
        public UInt64 ByteOffsetToWaveformData; //8b
        public UInt32 WaveformPacketSizeInBytes; //4b
        public float ReturnPointWaveformLocation; //4b
        public float Xt; //4b
        public float Yt; //4b
        public float Zt; //4b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos, ref data);
            this.WavePacketDescriptorIndex = ReadByte(ref pos, ref data);
            this.ByteOffsetToWaveformData = ReadUnsignedLongLong(ref pos, ref data);
            this.WaveformPacketSizeInBytes = ReadUnsignedLong(ref pos, ref data);
            this.ReturnPointWaveformLocation = ReadUnsignedLong(ref pos, ref data);
            this.Xt = ReadFloat(ref pos, ref data);
            this.Yt = ReadFloat(ref pos, ref data);
            this.Zt = ReadFloat(ref pos, ref data);
        }
    }

    /// <summary>
    /// Format 10 adds wave packets to format 7 - this is what is says in the specification document, but...
    /// format 10 also contains RGB, NIR and wave packets, so it's based on 8 (NIR) which is based on 7 (RGB) and I've added wave packets.
    /// </summary>
    public class LASPointDataRecordFormat10 : LASPointDataRecordFormat8
    {
        public byte WavePacketDescriptorIndex; //1b
        public UInt64 ByteOffsetToWaveformData; //8b
        public UInt32 WaveformPacketSizeInBytes; //4b
        public float ReturnPointWaveformLocation; //4b
        public float Xt; //4b
        public float Yt; //4b
        public float Zt; //4b
        public override void FromData(ref long pos, ref byte[] data)
        {
            base.FromData(ref pos, ref data);
            this.WavePacketDescriptorIndex = ReadByte(ref pos, ref data);
            this.ByteOffsetToWaveformData = ReadUnsignedLongLong(ref pos, ref data);
            this.WaveformPacketSizeInBytes = ReadUnsignedLong(ref pos, ref data);
            this.ReturnPointWaveformLocation = ReadUnsignedLong(ref pos, ref data);
            this.Xt = ReadFloat(ref pos, ref data);
            this.Yt = ReadFloat(ref pos, ref data);
            this.Zt = ReadFloat(ref pos, ref data);
        }
    }

    #endregion PointDataRecordFormats


    // Use this for initialization
    void Start () {
		
	}

    public void Awake()
    {
        //TODO: read and create the component here...
        ReadLASLidar();
        CreateTerrain();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    /// <summary>
    /// Do the work...
    /// </summary>
    protected void ReadLASLidar()
    {
        byte [] bytes = File.ReadAllBytes(Filename);
        Header = ReadLASHeader(ref bytes);
        ReadLASPoints(Header,ref bytes);
    }

    #region ProtectedReaderMethods
    //this region contains all the methods for reading chars, strings, longs, floats etc from the Lidar stream - basic bit reading and manipulation stuff
    //For reference: C# byte=1 byte, short=2 bytes, unsigned int=4 bytes, long=8 bytes
    //LAS defines: short=2 bytes, long=4 bytes, long long=8 bytes
    //I've used C# Uint16 (short), UInt32 (long) and UInt64 (long long)

    /// <summary>
    /// Read a single char from the byte array at position "pos" and update the pos counter
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="data"></param>
    /// <returns>The character and updated "pos" counter</returns>
    public static char ReadChar(ref long pos, ref byte [] data)
    {
        char ch = (char)data[pos];
        ++pos;
        return ch;
    }

    /// <summary>
    /// Read a fixed number of chars into a char array
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="data"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static char[] ReadChars(ref long pos, ref byte [] data, int size)
    {
        char[] Result = new char[size];
        for (int i = 0; i < size; i++)
        {
            Result[i] = (char)data[pos];
            ++pos;
        }

        return Result;
    }

    /// <summary>
    /// Read a fixed length string
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="data"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static string ReadString(ref long pos, ref byte [] data, int size)
    {
        //TODO: do you need to check for null padding?
        string s = Encoding.UTF8.GetString(data, (int)pos, size);
        pos += size;
        return s;
    }

    /// <summary>
    /// Read an unsigned short value. NOTE: in C# the ushort type is 32 bits (4 bytes). LAS defined an unsigned short as 2 bytes (16 bits).
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static UInt16 ReadUnsignedShort(ref long pos, ref byte [] data)
    {
        //2 bytes
        UInt16 us = BitConverter.ToUInt16(data, (int)pos);
        pos += 2;
        return us;
    }

    /// <summary>
    /// Read an short value. NOTE: in C# the short type is 32 bits (4 bytes). LAS defined a short as 2 bytes (16 bits).
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Int16 ReadShort(ref long pos, ref byte[] data)
    {
        //2 bytes
        Int16 s = BitConverter.ToInt16(data, (int)pos);
        pos += 2;
        return s;
    }

    /// <summary>
    /// Read unsigned long value. NOTE: in C# the ulong type is 64 bits (8 bytes). LAS defines an unsigned long as 4 bytes (32 bits).
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static UInt32 ReadUnsignedLong(ref long pos, ref byte[] data)
    {
        //4 bytes
        UInt32 ul = BitConverter.ToUInt32(data, (int)pos); //yes, it's supposed to be 32 bits wide!
        pos += 4;
        return ul;
    }

    /// <summary>
    /// Read unsigned long long value. NOTE: in C# the ulong type is 64 bits (8 bytes). LAS defines an unsigned long as 4 bytes (32 bits) and unsigned long long as 8 bytes (64 bits).
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static UInt64 ReadUnsignedLongLong(ref long pos, ref byte [] data)
    {
        //8 bytes
        UInt64 ull = BitConverter.ToUInt64(data, (int)pos);
        pos += 8;
        return ull;
    }

    public static Int32 ReadLong(ref long pos, ref byte [] data)
    {
        //4 bytes
        Int32 l = BitConverter.ToInt32(data, (int)pos);
        pos += 8;
        return l;
    }

    public static byte ReadByte(ref long pos, ref byte[] data)
    {
        byte b = data[pos];
        ++pos;
        return b;
    }

    public static float ReadFloat(ref long pos, ref byte [] data)
    {
        float f = BitConverter.ToSingle(data, (int)pos);
        pos += 4;
        return f;
    }

    public static double ReadDouble(ref long pos, ref byte [] data)
    {
        double d = BitConverter.ToDouble(data, (int)pos);
        pos += 8;
        return d;
    }

    #endregion ProtectedReaderMethods

    /// <summary>
    /// Read the LAS common header structure.
    /// NOTE: I don't want to compile /unsafe, so the only way to do this is by interpreting the byte stream directly. Normally I would used a fixed struct to do this.
    /// </summary>
    /// <param name="data"></param>
    /// <returns>A public header block strucuture which is filled in from the bytes in data[]</returns>
    protected LASPublicHeaderBlock ReadLASHeader(ref byte [] data)
    {
        LASPublicHeaderBlock header = new LASPublicHeaderBlock();

        long pos = 0; //current position in file
        header.FileSignature = ReadString(ref pos, ref data, 4);
        if (header.FileSignature!="LASF")
        {
            Debug.Log("LidarLASReader.ReadLASHeader: Error reading header, FileSignature expected 'LASF' but found '" + header.FileSignature + "'. Load aborted.");
            return header;
        }
        header.FileSourceID = ReadUnsignedShort(ref pos, ref data);
        header.GlobalEncoding = ReadUnsignedShort(ref pos, ref data);
        header.ProjectID1 = ReadUnsignedLong(ref pos, ref data);
        header.ProjectID2 = ReadUnsignedShort(ref pos, ref data);
        header.ProjectID3 = ReadUnsignedShort(ref pos, ref data);
        header.ProjectID4 = ReadString(ref pos, ref data, 8);
        header.VersionMajor = ReadByte(ref pos, ref data);
        header.VersionMinor = ReadByte(ref pos, ref data);
        header.SystemIdentifier = ReadString(ref pos, ref data, 32);
        header.GeneratingSoftware = ReadString(ref pos, ref data, 32);
        header.FileCreationDayofYear = ReadUnsignedShort(ref pos, ref data);
        header.FileCreationYear = ReadUnsignedShort(ref pos, ref data);
        header.HeaderSize = ReadUnsignedShort(ref pos, ref data);
        header.OffsetToPointData = ReadUnsignedLong(ref pos, ref data);
        header.NumberofVariableLengthRecords = ReadUnsignedLong(ref pos, ref data);
        header.PointDataRecordFormat = (byte)(ReadByte(ref pos, ref data) & 0x7f); //strip the top bit off as the unsigned char original type seems to set the MSB
        header.PointDataRecordLength = ReadUnsignedShort(ref pos, ref data);
        header.LegacyNumberofPointRecords = ReadUnsignedLong(ref pos, ref data);
        header.LegacyNumberofPointsByReturn = new UInt32 [] {
            ReadUnsignedLong(ref pos, ref data),
            ReadUnsignedLong(ref pos, ref data),
            ReadUnsignedLong(ref pos, ref data),
            ReadUnsignedLong(ref pos, ref data),
            ReadUnsignedLong(ref pos, ref data)
        };
        header.XScaleFactor = ReadDouble(ref pos, ref data);
        header.YScaleFactor = ReadDouble(ref pos, ref data);
        header.ZScaleFactor = ReadDouble(ref pos, ref data);
        header.XOffset = ReadDouble(ref pos, ref data);
        header.YOffset = ReadDouble(ref pos, ref data);
        header.ZOffset = ReadDouble(ref pos, ref data);
        header.MaxX = ReadDouble(ref pos, ref data);
        header.MinX = ReadDouble(ref pos, ref data);
        header.MaxY = ReadDouble(ref pos, ref data);
        header.MinY = ReadDouble(ref pos, ref data);
        header.MaxZ = ReadDouble(ref pos, ref data);
        header.MinZ = ReadDouble(ref pos, ref data);
        //TODO: these are only in the 1.4 spec files
        header.StartofWaveformDataPacketRecord = ReadUnsignedLongLong(ref pos, ref data);
        header.StartofFirstExtendedVariableLengthRecord = ReadUnsignedLong(ref pos, ref data); //See note above on long long values 
        header.NumberofExtendedVariableLengthRecords = ReadUnsignedLong(ref pos, ref data);
        header.NumberofPointRecords = ReadUnsignedLongLong(ref pos, ref data);
        header.NumberofPointsByReturn = new UInt64[15];
        for (int i = 0; i < 15; i++) header.NumberofPointsByReturn[i] = ReadUnsignedLongLong(ref pos, ref data);

        return header;
    }

    protected void ReadLASPoints(LASPublicHeaderBlock header,ref byte [] data)
    {
        ulong NumPoints = header.LegacyNumberofPointRecords; // header.NumberofPointRecords; //TODO: need to use the correct one based on version
        long pos = header.OffsetToPointData;
        uint PDRFormat = header.PointDataRecordFormat; //0..10 designates which format of point data is being used - all points are the same format (preferred formats in LAS 1.4 are 6-10)
        for (ulong i=0; i<NumPoints; i++)
        {
            LASPointDataRecordFormat P=null;
            switch (PDRFormat) {
                case 0: P = new LASPointDataRecordFormat0(); break;
                case 1: P = new LASPointDataRecordFormat1(); break;
                case 2: P = new LASPointDataRecordFormat2(); break;
                case 3: P = new LASPointDataRecordFormat3(); break;
                case 4: P = new LASPointDataRecordFormat4(); break;
                case 5: P = new LASPointDataRecordFormat5(); break;
                case 6: P = new LASPointDataRecordFormat6(); break;
                case 7: P = new LASPointDataRecordFormat7(); break;
                case 8: P = new LASPointDataRecordFormat8(); break;
                case 9: P = new LASPointDataRecordFormat9(); break;
                case 10: P = new LASPointDataRecordFormat10(); break;
            }
            P.FromData(ref pos, ref data);
            //apply scale and offset factor from header to point integer offset data to get final point
            double X = P.X * header.XScaleFactor + header.XOffset;
            double Y = P.Y * header.YScaleFactor + header.YOffset;
            double Z = P.Z * header.ZScaleFactor + header.ZOffset;
        }
    }

    protected void CreateTerrain()
    {
        //TODO:
    }
}
