
using System;
using System.IO;
using System.Text;

public class MemoryBuffer : MemoryStream
{

    public MemoryBuffer() {

    }
    public MemoryBuffer(byte[] buffer) : base(buffer) {

    }
    #region Short
    public short ReadShort() {
        byte[] arr = new byte[2];
        base.Read(arr, 0, arr.Length);
        return BitConverter.ToInt16(arr, 0);
    }
    public void WriteShort(short value) {
        byte[] arr = BitConverter.GetBytes(value);
        base.Write(arr, 0, arr.Length);
    }
    #endregion


    #region UShort
    public ushort ReadUShort() {
        byte[] arr = new byte[2];
        base.Read(arr, 0, arr.Length);
        return BitConverter.ToUInt16(arr, 0);
    }
    public void WriteUShort(ushort value) {
        byte[] arr = BitConverter.GetBytes(value);
        base.Write(arr, 0, arr.Length);
    }
    #endregion


    #region Int
    public int ReadInt() {
        byte[] arr = new byte[4];
        base.Read(arr, 0, arr.Length);
        return BitConverter.ToInt32(arr, 0);
    }
    public void WriteInt(int value) {
        byte[] arr = BitConverter.GetBytes(value);
        base.Write(arr, 0, arr.Length);
    }
    #endregion


    #region UInt
    public uint ReadUInt() {
        byte[] arr = new byte[4];
        base.Read(arr, 0, arr.Length);
        return BitConverter.ToUInt32(arr, 0);
    }
    public void WriteUInt(uint value) {
        byte[] arr = BitConverter.GetBytes(value);
        base.Write(arr, 0, arr.Length);
    }
    #endregion


    #region Float
    public float ReadFloat() {
        byte[] arr = new byte[4];
        base.Read(arr, 0, arr.Length);
        return BitConverter.ToSingle(arr, 0);
    }
    public void WriteFloat(float value) {
        byte[] arr = BitConverter.GetBytes(value);
        base.Write(arr, 0, arr.Length);
    }
    #endregion


    #region Double
    public double ReadDouble() {
        byte[] arr = new byte[8];
        base.Read(arr, 0, arr.Length);
        return BitConverter.ToDouble(arr, 0);
    }
    public void WriteDouble(double value) {
        byte[] arr = BitConverter.GetBytes(value);
        base.Write(arr, 0, arr.Length);
    }
    #endregion


    #region Bool
    public bool ReadBool() {
        return base.ReadByte() == 1;
    }
    public void WriteBool(bool value) {
        base.WriteByte((byte)(value == true ? 1 : 0));
    }
    #endregion


    #region String
    public string ReadString() {
        int len = this.ReadInt();
        byte[] arr = new byte[len];
        base.Read(arr, 0, len);
        return Encoding.UTF8.GetString(arr, 0, len);
    }
    public void WriteString(string value) {
        byte[] arr = Encoding.UTF8.GetBytes(value);
        this.WriteInt(arr.Length);
        base.Write(arr, 0, arr.Length);
    }
    #endregion


    #region Byte Array
    public byte[] ReadByteArray() {
        int len = this.ReadInt();
        byte[] arr = new byte[len];
        base.Read(arr, 0, len);
        return arr;
    }
    public void WriteByteArray(byte[] value) {
        this.WriteInt(value.Length);
        base.Write(value, 0, value.Length);
    }
    #endregion
}
