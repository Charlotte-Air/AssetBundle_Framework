using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using System.Diagnostics;
using System.Xml.Serialization;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Charlotte 
{
    public class VersionConfig  //版本控制类
    {
        string root_dir = null;
        public Dictionary<string, FileMD5> fileMD5s = null;

        public VersionConfig(string path) 
        {
            fileMD5s = new Dictionary<string, FileMD5>();
            root_dir = path + "/";
            Reload(path);
        }

        public VersionConfig() 
        {
            fileMD5s = new Dictionary<string, FileMD5>();
        }

        private void Reload(string path) 
        {
            string[] files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; i++) 
            {
                var file = files[i];
                if (file.EndsWith(".meta")) 
                {
                    continue;
                }
                var md5 = new FileMD5(FileMD5.ParamType.FileName, file);
                string key = PathToKey(file);
                fileMD5s.Add(key, md5);
            }
            string[] dirs = Directory.GetDirectories(path);
            for (int i = 0; i < dirs.Length; i++) 
            {
                Reload(dirs[i]);
            }
        }
        
        internal struct DataJson 
        {
            public string filePath;
            public string MD5;
            public long fileSize;
        }

        internal struct DataJsonList 
        {
            public List<DataJson> list;
        }
        
        public Hash128 GetHash(string path) 
        {
            string key = PathToKey(path);
            if (fileMD5s.ContainsKey(key)) 
            {
                return fileMD5s[key].ToHash128();
            }
            Debug.Log($"version config GetHash({path}) error: ");
            return new Hash128(0, 0, 0, 0);
        }

        public long GetFileSize(string path)
        {
            string key = PathToKey(path);
            if (fileMD5s.ContainsKey(key))
            {
                return fileMD5s[key].fileSize;
            }
            Debug.Log($"version config GetFileSize({path}) error ");
            return 0;
        }

        private string PathToKey(string path) 
        {
            path = path.Replace('\\', '/');
            if (path.StartsWith(root_dir))
            {
                path = path.Substring(root_dir.Length);
            }
            else
            {
#if UNITY_ANDROID
                string rootAssetPath = Application.streamingAssetsPath + "/" + AssetBundlePack.GetName(AssetBundlePack.PackType.Android) + "/";
                if (path.StartsWith(rootAssetPath))
                {
                    path = path.Substring(rootAssetPath.Length);
                }
#endif
            }
            return path;
        }
    }
        
    public class FileMD5 //文件MD5效应
    {
        public enum ParamType 
        {
            FileName = 0,
            MD5,
        };
        private byte[] byteVal;
        public long fileSize;
        public FileMD5(ParamType type, string param) 
        {
            switch (type) 
            {
                case ParamType.FileName:
                    byteVal = FileToBytes(param);
                    break;
                case ParamType.MD5:
                    byteVal = MD5ToBytes(param);
                    break;
            }
        }

        public bool IsValid() { return (byteVal != null); }

        private byte[] FileToBytes(string file) {
            try {
                if (!File.Exists(file)) {
                    return null;
                }

                FileStream fileStream = new FileStream(file, FileMode.Open);
                fileSize = fileStream.Length;
                var md5Tool = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5Tool.ComputeHash(fileStream);
                fileStream.Close();
                return retVal;
            } catch (Exception e) {
                Debug.LogError("Failed to Calculate Md5 from " + file + "!" + e.ToString());
                return null;
            }
        }

        private byte[] MD5ToBytes(string md5) {
            byte[] retVal = CommonTool.HexToBytes(md5);
            return retVal;
        }

        public override string ToString() {
            if (!IsValid()) {
                return "";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < byteVal.Length; i++) {
                sb.Append(byteVal[i].ToString("x2"));
            }
            return sb.ToString().ToLower();
        }

        public Hash128 ToHash128() {
            uint[] _params = new uint[4];
            int count = 0;
            int idx = 0;
            uint val = 0;
            for (int i = 0; i < byteVal.Length; i++) {
                val += (uint)(byteVal[i] << 8 * count);
                if (++count >= 4) {
                    _params[idx++] = val;
                    val = 0;
                    count = 0;

                    if (idx >= 4) {
                        break;
                    }
                }
            }

            return new Hash128(_params[0], _params[1], _params[2], _params[3]);
        }
    }
    
    public class CommonTool //通用工具类
    {
        static byte urlSpace = 43;
        static byte urlEscapeChar = 37;
        static byte[] ucHexChars = Encoding.ASCII.GetBytes("0123456789ABCDEF");
        static byte[] lcHexChars = Encoding.ASCII.GetBytes("0123456789abcdef");
        static byte[] urlForbidden = Encoding.ASCII.GetBytes("@&;:<>=?\"'/\\!#%+$,{}|^[]`");
        static Vector3[] _SphereCastOffset = new Vector3[] { Vector3.zero, Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        
        public static byte[] HexToBytes(string hex) 
        {
            string fixedHex = hex.Replace("-", string.Empty);
            // array to put the result in
            byte[] bytes = new byte[fixedHex.Length / 2];
            // variable to determine shift of high/low nibble
            int shift = 4;
            // offset of the current byte in the array
            int offset = 0;
            // loop the characters in the string
            foreach (char c in fixedHex) {
                // get character code in range 0-9, 17-22
                // the % 32 handles lower case characters
                int b = (c - '0') % 32;
                // correction for a-f
                if (b > 9)
                    b -= 7;
                // store nibble (4 bits) in byte array
                bytes[offset] |= (byte)(b << shift);
                // toggle the shift variable between 0 and 4
                shift ^= 4;
                // move to next byte
                if (shift != 0)
                    offset++;
            }
            return bytes;
        }

        public static string BytesToHex(byte[] bytes) 
        {
            string hex = BitConverter.ToString(bytes);
            hex = hex.Replace("-", "");
            return hex;
        }

        public static string ToXmlString<T>(T t) 
        {
            using (StringWriter w = new StringWriter()) 
            {
                XmlSerializer x = new XmlSerializer(t.GetType());
                x.Serialize(w, t);
                return w.ToString();
            }
        }

        // AES加密,密钥长度为128,192或256位
        public static string AesEncrypt(string toEncrypt, string key = "12345678901234567890123456789012") 
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        // AES解密,密钥长度为128,192或256位
        public static string AesDecrypt(string toDecrypt, string key = "12345678901234567890123456789012") 
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);
            try 
            {
                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return UTF8Encoding.UTF8.GetString(resultArray);
            } 
            catch (System.Security.Cryptography.CryptographicException e) 
            {
                Debug.LogError(e.ToString());
            }
            return string.Empty;
        }

        [Conditional("SKY_DEBUG")]
        public static void DebugSphereLine(Vector3 start, Vector3 dir, float radius, float len, Color centerColor, Color lineColor, float duration = 1, float angleStep = 45) 
        {
            Vector3 pos = start;
            Vector3 tmp;
            UnityEngine.Debug.DrawLine(pos, pos + dir * len, centerColor, duration);
            for (float angle = 0; angle < 360; angle += angleStep) 
            {
                Quaternion ang = Quaternion.AngleAxis(angle, dir);
                tmp = pos + ang * Vector3.up * radius;
                UnityEngine.Debug.DrawLine(tmp, tmp + dir * len, lineColor, duration);
            }
        }

        [Conditional("SKY_DEBUG")]
        public static void DebugSphere(Vector3 centerPos, float radius, Color sphereColor, Color centerColor, float stepCount = 20, float duration = 1) 
        {
            float alpha = 0;
            float beta = 0;
            float step = (float)Math.PI * 2 / stepCount;
            Vector3 lastPos = new Vector3((float)(Math.Sin(alpha) * Math.Sin(beta)), (float)(Math.Cos(alpha) * Math.Sin(beta)), (float)Math.Cos(beta)) * radius + centerPos;
            UnityEngine.Debug.DrawLine(centerPos, lastPos, centerColor, duration);
            Vector3 pos;
            for (beta = 0; beta < Math.PI * 2; beta += step) 
            {
                for (alpha = -(float)Math.PI + 0.01f; alpha <= (float)Math.PI; alpha += step) 
                {
                    pos = new Vector3((float)(Math.Sin(alpha) * Math.Sin(beta)), (float)(Math.Cos(alpha) * Math.Sin(beta)), (float)Math.Cos(beta)) * radius + centerPos;
                    UnityEngine.Debug.DrawLine(lastPos, pos, sphereColor, duration);
                    lastPos = pos;
                }
            }
            UnityEngine.Debug.DrawLine(centerPos, lastPos, centerColor, duration);
        }
        
        // 检测是否被阻挡,参数:startPos-起始点,targetPos-终点,radius-检测球半径,blockLayerMask-碰撞层掩码,isWhole-是否要求整体都能通过
        public static bool IsSpherePass(Vector3 startPos, Vector3 targetPos, float radius, int blockLayerMask = 1, bool isWhole = false) 
        {
            Vector3 offset;
            if (isWhole) 
            {
                for (int i = 0; i < _SphereCastOffset.Length; i++) 
                {
                    offset = _SphereCastOffset[i] * radius;
                    if (Physics.Linecast(startPos + offset, targetPos + offset, blockLayerMask)) 
                    {
                        return false;
                    }
                }
                return true;
            } 
            else 
            {
                for (int i = 0; i < _SphereCastOffset.Length; i++) 
                {
                    offset = _SphereCastOffset[i] * radius;
                    if (!Physics.Linecast(startPos + offset, targetPos + offset, blockLayerMask)) 
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        
        public static GameObject CreateSingleton(string name, bool isDontDestroy = true) 
        {
            GameObject obj = new GameObject();
            obj.name = string.Format("____@{0}(System Create)", name);
            if (isDontDestroy) 
            {
                UnityEngine.Object.DontDestroyOnLoad(obj);
            }
            return obj;
        }

#if !UNITY_WEBPLAYER
        /// <summary>
        /// 计算文件的md5值，返回大写格式
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GenerateFileMD5Upper(string file) 
        {
            if (File.Exists(file) == false) 
            {
                return string.Empty;
            }
            byte[] fileByte = File.ReadAllBytes(file);
            if (fileByte == null) 
            {
                return string.Empty;
            }
            byte[] hashByte = new MD5CryptoServiceProvider().ComputeHash(fileByte);			
            return ByteArrayToString(hashByte);
        }
#endif

        /// <summary>
        /// 输出数据的十六进制字符串
        /// </summary>
        /// <param name="arrInput"></param>
        /// <returns></returns>
        public static string ByteArrayToString(byte[] arrInput) 
        {
            StringBuilder sOutput = new StringBuilder(arrInput.Length);
            for (int i = 0; i < arrInput.Length; i++) 
            {
                sOutput.Append(arrInput[i].ToString("X2"));
            }
            return sOutput.ToString();
        }

        public static string GetNetIP() 
        {
            string strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            return (ipEntry != null && ipEntry.AddressList.Length > 0) ? ipEntry.AddressList[0].ToString() : "";
        }

        static byte[] Byte2Hex(byte b, byte[] hexChars) 
        {
            return new byte[]
            {
                hexChars[b >> 4],
                hexChars[(int)(b & 15)]
            };
        }

        public static byte[] Encode(byte[] input, byte escapeChar, byte space, byte[] forbidden, bool uppercase) 
        {
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream(input.Length * 2)) 
            {
                for (int i = 0; i < input.Length; i++) 
                {
                    if (input[i] == 32) 
                    {
                        memoryStream.WriteByte(space);
                    }
                    else if (input[i] < 32 || input[i] > 126)// || ByteArrayContains(forbidden, input[i]))
                    {
                        memoryStream.WriteByte(escapeChar);
                        memoryStream.Write(Byte2Hex(input[i], (!uppercase) ? lcHexChars : ucHexChars), 0, 2);
                    } 
                    else 
                    {
                        memoryStream.WriteByte(input[i]);
                    }
                }
                result = memoryStream.ToArray();
            }
            return result;
        }

        static bool ByteArrayContains(byte[] array, byte b) 
        {
            int num = array.Length;
            for (int i = 0; i < num; i++) 
            {
                if (array[i] == b) 
                {
                    return true;
                }
            }
            return false;
        }

        public static string URLEncode(string toEncode, Encoding e) 
        {
            byte[] array = Encode(e.GetBytes(toEncode), urlEscapeChar, urlSpace, urlForbidden, false);
            return Encoding.ASCII.GetString(array, 0, array.Length);
        }

        public static string ToUTF8(string src)
        {
            var utf8 = Encoding.UTF8;
            byte[] srcBytes = Encoding.Unicode.GetBytes(src);
            byte[] dstBytes = Encoding.Convert(Encoding.Unicode, utf8, srcBytes);
            return utf8.GetString(dstBytes, 0, dstBytes.Length);
        }
        
        /*
        public static T ToObject<T>(String jsonTxt) 
        {
// #if SKY_JSON_DOT_NET
//#if UNITY_EDITOR
            try 
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonTxt);
            } 
            catch(Newtonsoft.Json.JsonReaderException e) 
            {
                Debug.LogError($"解析Json字符串格式错误,原始字符串[{jsonTxt}],出错行号:{1},出错列号:{e.LineNumber},原始错误信息:{e.ToString()}");
                return default(T);
            } 
            catch(Newtonsoft.Json.JsonSerializationException e) 
            {
                Debug.LogError($"解析Json字符串格式错误,错误原因:将列表解析为单个对象,原始字符串[{jsonTxt}],原始错误信息:{e.ToString()}");
                return default(T);
            }
// #else
//             try 
//             {
//                 return JsonUtility.FromJson<T>(jsonTxt);
//             }
//             catch(Exception e) 
//             {
//                 Debug.LogError("解析Json字符串格式错误,原始字符串[{0}],原始错误信息:{1} " +  jsonTxt + " " +  e.ToString());
//                 return default(T);
//             }
// #endif
        }
        public static string ToJson<T>(T obj) 
        {
#if UNITY_EDITOR
            try 
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            } 
            catch (Newtonsoft.Json.JsonReaderException e) 
            {
                string rv = string.Format("ToJson错误,对象类型[{0}],出错行号:{1},出错列号:{2},错误信息:{3}", (obj == null ? "null" : obj.GetType().Name), e.LineNumber, e.LinePosition, e.ToString());
                Debug.LogError(rv);
                return rv;
            } 
            catch (Newtonsoft.Json.JsonSerializationException e) 
            {
                string rv = string.Format("ToJson错误,对象类型[{0}],错误信息:{1}", (obj == null ? "null" : obj.GetType().Name), e.ToString());
                Debug.LogError(rv);
                return rv;
            }
#else
            try
            {
                return JsonUtility.ToJson(obj);
            } 
            catch (Exception e) 
            {
                string rv = string.Format("ToJson错误,对象类型[{0}],错误信息:{1}", (obj == null ? "null" : obj.GetType().Name), e.ToString());
                Debug.LogError(rv);
                return rv;
            }
#endif
        }

        // 将对象转换为Bson
        public static byte[] ToBson<T>(T obj) 
        {
// #if SKY_JSON_DOT_NET
#if UNITY_EDITOR
            using (var stream = new MemoryStream()) 
            {
                using (Newtonsoft.Json.Bson.BsonWriter writer = new Newtonsoft.Json.Bson.BsonWriter(stream)) 
                {
                    Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
                    serializer.Serialize(writer, obj);
                }
                return stream.ToArray();
                //var serialized = Convert.ToBase64String(serializedData);
            }
#else
            return StringTool.byteEncoding.GetBytes(ToJson(obj));
#endif
        }

        // 将Bson转换为对象
        public static T ToObject<T>(byte[] serializedData) 
        {
// #if SKY_JSON_DOT_NET
#if UNITY_EDITOR
            using (var stream = new MemoryStream(serializedData)) 
            {
                using (Newtonsoft.Json.Bson.BsonReader reader = new Newtonsoft.Json.Bson.BsonReader(stream)) 
                {
                    Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
                    return serializer.Deserialize<T>(reader);
                }
            }
#else
            return ToObject<T>(StringTool.byteEncoding.GetString(serializedData));
#endif
        }
        */
    }
}