using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using Mono.Data.Sqlite;
using System.Runtime.InteropServices;

//public interface IReleasable 
//{
//    void Release();
//}

//解决在模板类中有多个静态对象问题
public class StaticValueContainerDBFRAM
{
    protected static string DataBaseName = "HpyFishDB.db";
    protected static SqliteConnection mConn;
    protected static int SqliteConnRefCount = 0;//引用计数
    protected static SqliteCommand mCMD;
    protected static uint CurrentUseAddress = 16;//当前最后使用的存储地址,如果是0xffffffff的话表示无可用地址了.0~15是guid
    protected static System.Guid DB_ID ;//数据库和铁电的对应id
    protected static bool IsInitFRAMByDB = false;//是否从数据库更新FRAM,如果为true,无论数据库是否初始化都更新FRAM数据

    public static System.Guid GetDBID()
    {
        return DB_ID;
    }
}
/// <summary>
/// 数据库和铁电混合存储方式
/// </summary>
/// <remarks>
/// 注意:
/// 1.需要使用到NemoSerialHardScan串口通信类
/// 2.[BUG]如果已经建立完整的数据表,比如有100个表,如果再创建多一个,地址的分配会出现问题
/// 3.不支持string
/// </remarks>
/// <typeparam name="ValueType"></typeparam>

public class HotFileDbFRAMIO<ValueType> : StaticValueContainerDBFRAM, IReleasable, HotFileDBFRAM_Updater.IUpdatable// where ValueType : uint, int, bool, ushort, long, short, ulong, float, double, string, new()//
{
    
    private string mKey;//键,对应数据库中的表
    //private bool mFileInited = false;//文件已经初始化,路劲,等数据已准备 
    //private static ValueType DefVal;
    private System.Data.DbType mDBType;//数据库类型
    private string mUpdateSQLText;

    private INemoControlIO mMCU;
    private uint mAddress;
    private byte mDataLength;
    public HotFileDbFRAMIO(string key, string filePath)
    {
        if(GameMain.Singleton != null)
            mMCU = GameMain.Singleton.ArcIO;
        if(mMCU == null)
            mMCU = INemoControlIOSinglton.Get();
        //初始化数据连接
        if (mConn == null)
        {
            
            if (DataBaseName == "")
                DataBaseName = "GameDB.db";

            string dataPath = System.Environment.CurrentDirectory + "/DataFiles";
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }


            mConn = new SqliteConnection("Data Source = DataFiles/" + DataBaseName + ";");
            mConn.Open();

            if (mCMD == null)
            {
                mCMD = mConn.CreateCommand();
                mCMD.Parameters.Add(new SqliteParameter());
            }

            mCMD.CommandText = "PRAGMA journal_mode =wal;";//wal模式
            mCMD.ExecuteNonQuery();
        }


 
        mKey = key;
        mDBType = GetDBType();
        //bool isInitFRAMByDB = false;
        //初始化DB_FARM_ID
        if (mMCU.IsOpen() && DB_ID == System.Guid.Empty)
        {
            using (SqliteCommand cmd = mConn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS _DB_GUID(Val BLOB);";
                //cmd.Parameters.Add("@data", System.Data.DbType.Binary).Value = System.Guid.Empty.ToByteArray();
                cmd.ExecuteNonQuery();
                cmd.CommandText = string.Format(" SELECT * FROM _DB_GUID;");
                SqliteDataReader r = cmd.ExecuteReader();
                r.Read();

                if (r.HasRows)//在数据库已经存在该值
                {
                    //读取数据库内的guid值
                    DB_ID = new System.Guid(r.GetValue(0) as byte[]);
                    //读取FRAM的guid值
                    byte[] guidFRAMByte = null;
                    if (mMCU.Read_Block(0, 16, out guidFRAMByte))//从FRAM读取guid成功
                    {
                        System.Guid FRAMEGUID = new System.Guid(guidFRAMByte);
                        if (FRAMEGUID != DB_ID)//FRAM和db的guid不一致
                        {
                            mMCU.RequestReadWrite(true, 0, 16, DB_ID.ToByteArray());//将dbid写入FRAM
                            IsInitFRAMByDB = true;
                        }
                    }
                    r.Close();
                }
                else//数据库不存在guid
                {
                    r.Close();
                    //创建新dbGUID
                    DB_ID = System.Guid.NewGuid();
                    byte[] guidByteNew = DB_ID.ToByteArray();
                    //存入db
                    cmd.CommandText = "INSERT INTO _DB_GUID VALUES (@data)";
                    cmd.Parameters.Add("@data", System.Data.DbType.Binary).Value = guidByteNew;
                    cmd.ExecuteNonQuery();

                    //将新创建的guid 存入FRAM
                    mMCU.RequestReadWrite(true, 0, 16, guidByteNew);
                    IsInitFRAMByDB = true;

                }
            }
        }


        //创建表(4294967295 == 0xffffffff)
        mCMD.CommandText = string.Format("CREATE TABLE IF NOT EXISTS {0:s}(Val {1:s} KEY DEFAULT {2:s},Address INTEGER DEFAULT 4294967295,Length INTEGER DEFAULT {3:d});", mKey, GetDBTypeString(), GetDefaultDBValString(),GetValLength());
        mCMD.ExecuteNonQuery();
 
        
        //查询是否存在数据行
        mCMD.CommandText = string.Format(" SELECT * FROM {0:s};", mKey);
        SqliteDataReader reader = mCMD.ExecuteReader();
        reader.Read();

        //
        //System.Guid.NewGuid();

        if (reader.HasRows)//在数据库已经存在该值
        {
            //从数据库读取地址和数据长度信息
            mAddress = (uint)reader.GetInt64(1);
            mDataLength = (byte)reader.GetInt32(2);
            if (IsInitFRAMByDB)
            {
                WriteToFRAM(ObjectToVal(reader.GetValue(0)));
            }
            reader.Close();
        }
        else//在数据库不存在该数据,需要添加行
        {
            mAddress = CurrentUseAddress;
            mDataLength = (byte)GetValLength();
            reader.Close();

            //初始化数据库表
            mCMD.CommandText = string.Format("INSERT INTO {0:s} VALUES({1:s},{2:d},{3:d})", mKey, GetDefaultDBValString(), mAddress, mDataLength);
            CurrentUseAddress += GetValLength();//递增地址
            mCMD.ExecuteNonQuery();

            //初始化farm
            WriteToFRAM(GetDefaultVal());

        }
       
        mUpdateSQLText = "UPDATE " + mKey + " SET Val = ?;";

        ++SqliteConnRefCount;
        HotFileDBIOReleaser.HotFileDBReg(this);
        HotFileDBFRAM_Updater.Reg(this);

    }
 

    public void Release()
    {
        --SqliteConnRefCount;
        if (SqliteConnRefCount == 0)
        {
            //Debug.Log("Close Conn");
            if (mCMD != null)
            {
                mCMD.Dispose();//如果不调用该函数,会导致不能关闭数据库
                mCMD = null;
            }
            if (mConn != null)
            {
                mConn.Close();
                mConn.Dispose();
                mConn = null;
            }
            
        }
    }

    /// <summary>
    /// 当MCU链接时,由HotFileDBFRAM_Updater判断并更新到FRAM
    /// </summary>
    public void UpdateToFRAM()
    {
        
        mCMD.CommandText = string.Format(" SELECT * FROM {0:s};", mKey);
        WriteToFRAM(ObjectToVal(mCMD.ExecuteScalar()));
    }
    /// <summary>
    /// 清扫文件(清空文件内容)
    /// </summary>
    public void Clearup()
    {
        //重置文件
        //ValueType tmpVal = Read();

        //mFileDescriptor = _wopen(mFileFullPath, 0x0200 | 0x0001, 0x0080 | 0x0100);//trunc|writeOnly,read|write
        //_close(mFileDescriptor);
        //mFileDescriptor = 0;

        //FlushWrite(tmpVal);
    }


    public ValueType Read()
    {
        //bool isNeedReadFromDB = true;//是否需要从数据库读取


        if (mMCU.IsOpen())//从mcu读取
        {
            //mMCU.RequestReadWrite(false, mAddress, mDataLength, null);//请求读取信息
            //float startTime = Time.realtimeSinceStartup;
            //bool isReaded = false;//是否已经读取到信息
            //byte[] dataReaded = null;
            //NemoCtrlIO_EventResultReadWrite delegateResultRead = (bool IsWrite, uint ads, byte datalen,byte resultcode, byte[] data) => 
            //{
            //    //Debug.Log(string.Format("delegateResultRead IsWrite:{0} adress:{1:d} resultCode:{2:d}", IsWrite, ads, resultcode));
            //    if (!IsWrite && ads == mAddress && resultcode==1)
            //    {
            //        isReaded = true;
            //        dataReaded = new byte[datalen];
            //        System.Array.Copy(data, dataReaded,datalen);
            //    }
            //};

            //mMCU.EvtResultReadWrite += delegateResultRead;//订阅
            //while (!isReaded)
            //{
            //    if (Time.realtimeSinceStartup - startTime > 5F)//时间限制
            //        break;
            //    //Debug.Log("Time.realtimeSinceStartup - startTime =" + (Time.realtimeSinceStartup - startTime));
            //    mMCU.RecivePackages();
            //    //System.Threading.Thread.Sleep(1);
            //}
            //mMCU.EvtResultReadWrite -= delegateResultRead;//退订

            ////从mcu读取成功 
            //if (isReaded)
            //{
            //    return ByteDataToVal(dataReaded);
            //}
            
            //测试
            byte[] data = null;
            if (mMCU.Read_Block(mAddress, mDataLength, out data))
            {
                return ByteDataToVal(data);
            }

        }

        //从mcu读取不成功
        mCMD.CommandText = string.Format(" SELECT * FROM {0:s};", mKey);
        //object outVal = mCMD.ExecuteScalar();
        return ObjectToVal(mCMD.ExecuteScalar());
    }

    /// <summary>
    /// 刷写
    /// </summary>
    /// <param name="value"></param>
    public void FlushWrite(ValueType value)
    {
        try
        {
            //if (mMCU.IsOpen())
            //{
            //    mMCU.RequestReadWrite(true, mAddress, mDataLength,ValToByteData(value));
            //}

            mCMD.CommandText = mUpdateSQLText;
            mCMD.Parameters[0].DbType = mDBType;
            mCMD.Parameters[0].Value = (object)value;
     
            mCMD.ExecuteNonQuery();
        }
        catch (System.Exception ex)
        { 
            Debug.LogError(ex.Message);
        }

    } 

    public void WriteToFRAM(ValueType value)
    {
        //return;
        if (mMCU.IsOpen())
        {
            //Debug.Log(string.Format("WriteToFRAM adress:{0:d}  dataLen:{1:d} data:{2}", mAddress, mDataLength, NemoSerial.ByteArrayToString(ValToByteData(value))));
            mMCU.RequestReadWrite(true, mAddress, mDataLength, ValToByteData(value)); 
        } 
    }
    private ValueType GetDefaultVal()
    {
        if (typeof(ValueType) == typeof(int))
        {
            return (ValueType)(object)(int)0;
        }

        if (typeof(ValueType) == typeof(uint))
        {
            return (ValueType)(object)(uint)0;
        }
        if (typeof(ValueType) == typeof(bool))
        {
            return (ValueType)(object)false;
        }

        if (typeof(ValueType) == typeof(ushort))
        {
            return (ValueType)(object)(ushort)0;
        }

        if (typeof(ValueType) == typeof(long))
        {
            return (ValueType)(object)(long)0;
        }

        if (typeof(ValueType) == typeof(short))
        {
            return (ValueType)(object)(short)0;
        }

        if (typeof(ValueType) == typeof(ulong))
        {
            return (ValueType)(object)(ulong)0;
        }

        if (typeof(ValueType) == typeof(float))
        {
            return (ValueType)(object)(float)0;
        }
        if (typeof(ValueType) == typeof(double))
        {
            return (ValueType)(object)(double)0;
        }

        if (typeof(ValueType) == typeof(string))
        {
            return (ValueType)(object)(string)"";
        }
        return (ValueType)(object)0;
    }
    private string GetDefaultDBValString()
    {
        if (typeof(ValueType) == typeof(int)
                    || typeof(ValueType) == typeof(uint)
                    || typeof(ValueType) == typeof(bool)
                    || typeof(ValueType) == typeof(ushort)
                    || typeof(ValueType) == typeof(long)
                    || typeof(ValueType) == typeof(short)
                    || typeof(ValueType) == typeof(ulong)
                    )
        {
            return "0";
        }

        if (typeof(ValueType) == typeof(float)
            || typeof(ValueType) == typeof(double))
        {
            return "0.0";
        }

        if (typeof(ValueType) == typeof(string))
        {
            return "''";
        }
        return "0";
    }
    private string GetDBTypeString()
    {
        if (typeof(ValueType) == typeof(int)
                    || typeof(ValueType) == typeof(bool)
                    || typeof(ValueType) == typeof(ushort)
                    || typeof(ValueType) == typeof(short)
                    )
        {
            return "INTEGER";
        }

        if(typeof(ValueType) == typeof(uint)
                    || typeof(ValueType) == typeof(long)
                    || typeof(ValueType) == typeof(ulong)
            )
        {
            return "BIGINT";
        }

        if (typeof(ValueType) == typeof(float)
            || typeof(ValueType) == typeof(double))
        {
            return "REAL";
        }

        if (typeof(ValueType) == typeof(string))
        {
            return "TEXT";
        }

        return "INTEGER";
    }

    private System.Data.DbType GetDBType()
    {
        if (typeof(ValueType) == typeof(int))
            return System.Data.DbType.Int32;
        else if(typeof(ValueType) == typeof(bool))
            return System.Data.DbType.Boolean;
        else if(typeof(ValueType) == typeof(ushort))
            return System.Data.DbType.UInt16;
        else if(typeof(ValueType) == typeof(long)
            || typeof(ValueType) == typeof(uint))
            return System.Data.DbType.Int64;
        else if(typeof(ValueType) == typeof(short))
            return System.Data.DbType.Int16;
        else if(typeof(ValueType) == typeof(ulong))
            return System.Data.DbType.UInt64;
        else if(typeof(ValueType) == typeof(float))
            return System.Data.DbType.Single;
        else if(typeof(ValueType) == typeof(double))
            return System.Data.DbType.Double;

        return System.Data.DbType.Int32;
    }


    private uint GetValLength()
    {
        if (typeof(ValueType) == typeof(int))
            return 4;
        else if (typeof(ValueType) == typeof(bool))
            return 1;
        else if (typeof(ValueType) == typeof(ushort))
            return 2;
        else if (typeof(ValueType) == typeof(long))
            return 8;
        else if (typeof(ValueType) == typeof(uint))
            return 4;
        else if (typeof(ValueType) == typeof(short))
            return 2;
        else if (typeof(ValueType) == typeof(ulong))
            return 8;
        else if (typeof(ValueType) == typeof(float))
            return 4;
        else if (typeof(ValueType) == typeof(double))
            return 8;

        return 4;
    }
    private ValueType ObjectToVal(object val)
    {
        if (typeof(ValueType) == typeof(int))
        {
            return (ValueType)(System.Object)System.Convert.ToInt32(val);
        }
        else if (typeof(ValueType) == typeof(float))
        {
            return (ValueType)(System.Object)System.Convert.ToSingle(val);
        }
        else if (typeof(ValueType) == typeof(bool))
        {
            return (ValueType)(System.Object)System.Convert.ToBoolean(val);
        }
        else if (typeof(ValueType) == typeof(long))
        {
            return (ValueType)(System.Object)System.Convert.ToInt64(val);
        }

        else if (typeof(ValueType) == typeof(ushort))
        {
            return (ValueType)(System.Object)System.Convert.ToUInt16(val);
        }
        else if (typeof(ValueType) == typeof(uint))
        {
            return (ValueType)(System.Object)System.Convert.ToUInt32(val);
        }

        else if (typeof(ValueType) == typeof(char))
        {
            return (ValueType)(System.Object)System.Convert.ToChar(val);
        }
        else if (typeof(ValueType) == typeof(double))
        {
            return (ValueType)(System.Object)System.Convert.ToDouble(val);
        }

        else if (typeof(ValueType) == typeof(short))
        {
            return (ValueType)(System.Object)System.Convert.ToInt16(val);
        }
        else if (typeof(ValueType) == typeof(ulong))
        {
            return (ValueType)(System.Object)System.Convert.ToUInt64(val);
        }
        return (ValueType)(System.Object)0;
    }


    private ValueType ByteDataToVal(byte[] data)
    {
        if (typeof(ValueType) == typeof(int))
        {
            return (ValueType)(System.Object)System.BitConverter.ToInt32(data,0);
        }
        else if (typeof(ValueType) == typeof(float))
        {
            return (ValueType)(System.Object)System.BitConverter.ToSingle(data, 0);
        }
        else if (typeof(ValueType) == typeof(bool))
        {
            return (ValueType)(System.Object)System.BitConverter.ToBoolean(data, 0);
        }
        else if (typeof(ValueType) == typeof(long))
        {
            return (ValueType)(System.Object)System.BitConverter.ToInt64(data, 0);
        }

        else if (typeof(ValueType) == typeof(ushort))
        {
            return (ValueType)(System.Object)System.BitConverter.ToUInt16(data, 0);
        }
        else if (typeof(ValueType) == typeof(uint))
        {
            return (ValueType)(System.Object)System.BitConverter.ToUInt32(data, 0);
        }

        else if (typeof(ValueType) == typeof(char))
        {
            return (ValueType)(System.Object)System.BitConverter.ToChar(data, 0);
        }
        else if (typeof(ValueType) == typeof(double))
        {
            return (ValueType)(System.Object)System.BitConverter.ToDouble(data, 0);
        }

        else if (typeof(ValueType) == typeof(short))
        {
            return (ValueType)(System.Object)System.BitConverter.ToInt16(data, 0);
        }
        else if (typeof(ValueType) == typeof(ulong))
        {
            return (ValueType)(System.Object)System.BitConverter.ToUInt64(data, 0);
        }
        return (ValueType)(System.Object)0;

    }


    private byte[] ValToByteData(ValueType val)
    {
        if (typeof(ValueType) == typeof(int))
        {
            return System.BitConverter.GetBytes(((int)(object)val));
        }
        else if (typeof(ValueType) == typeof(float))
        {
            return System.BitConverter.GetBytes(((float)(object)val));
        }
        else if (typeof(ValueType) == typeof(bool))
        {
            return System.BitConverter.GetBytes(((bool)(object)val));
        }
        else if (typeof(ValueType) == typeof(long))
        {
            return System.BitConverter.GetBytes(((long)(object)val));
        }

        else if (typeof(ValueType) == typeof(ushort))
        {
            return System.BitConverter.GetBytes(((ushort)(object)val));
        }
        else if (typeof(ValueType) == typeof(uint))
        {
            return System.BitConverter.GetBytes(((uint)(object)val));
        }

        else if (typeof(ValueType) == typeof(char))
        {
            return System.BitConverter.GetBytes(((char)(object)val));
        }
        else if (typeof(ValueType) == typeof(double))
        {
            return System.BitConverter.GetBytes(((double)(object)val));
        }

        else if (typeof(ValueType) == typeof(short))
        {
            return System.BitConverter.GetBytes(((short)(object)val));
        }
        else if (typeof(ValueType) == typeof(ulong))
        {
            return System.BitConverter.GetBytes(((ulong)(object)val));
        }
        
        return null;
    }
}
