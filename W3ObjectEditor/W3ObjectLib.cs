using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W3ObjectEditor
{
    /// <summary>
    /// 유틸리티 함수 모음 클래스 (ID 패딩, 타입 코드 파싱, 문자열 처리 등)
    /// </summary>
    public class Util
    {
        /// <summary>
        /// 문자열을 4바이트 길이로 고정 (부족하면 공백 패딩)
        /// </summary>
        public static string Fix4(string s)
        {
            if (s.Length >= 4) return s.Substring(0, 4);
            return s.PadRight(4, ' ');
        }

        /// <summary>
        /// 문자열 타입 코드를 정수형 코드로 변환
        /// </summary>
        public static int ParseTypeCode(string typeStr)
        {
            switch (typeStr)
            {
                case "int": return 0;
                case "real": return 1;
                case "unreal": return 2;
                case "string": return 3;
                default:
                    int result;
                    if (int.TryParse(typeStr.Replace("type", ""), out result))
                        return result;
                    return 0;
            }
        }
        public static string ReadNullTerminatedString(BinaryReader br)
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = br.ReadByte()) != 0)
                bytes.Add(b);
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        /// <summary>
        /// 문자열을 null 종료 문자열로 바이너리로 기록
        /// </summary>
        public static void WriteNullTerminatedString(BinaryWriter bw, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            bw.Write(bytes);
            bw.Write((byte)0);
        }
    }

    /// <summary>
    /// 객체의 단일 속성 수정 정보를 담는 클래스
    /// </summary>
    public class ObjectModification
    {
        public string FieldID { get; set; }
        public int ValueType { get; set; }
        public object Value { get; set; }
    }

    /// <summary>
    /// 객체 하나에 대한 전체 수정 내용을 담는 클래스
    /// </summary>
    public class W3Object
    {
        public string Source { get; set; }
        public string OriginalID { get; set; }
        public string NewID { get; set; }
        public List<ObjectModification> Modifications { get; set; }

        public W3Object()
        {
            Modifications = new List<ObjectModification>();
        }
    }

    /// <summary>
    /// .w3u 또는 .w3a 오브젝트 파일을 로드/저장하는 기능을 제공하는 클래스
    /// </summary>
    public static class W3ObjectFileHandler
    {
        private static string GetObjectType(string path) => Path.GetExtension(path).ToLowerInvariant();

        private static bool UsesLevelAndPointer(string ext) => ext == ".w3a"; //|| ext == ".w3h";

        public static async Task<DataTable> LoadAsync(string path)
        {
            return await Task.Run(() => Load(path));
        }

        public static DataTable Load(string path)
        {
            var ext = GetObjectType(path);
            var dt = CreateDataTable();
            bool useLevel = UsesLevelAndPointer(ext);

            using (var br = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                int version = br.ReadInt32();
                ReadTable(br, "Original", dt, useLevel);
                ReadTable(br, "Custom", dt, useLevel);
            }

            return dt;
        }

        public static void Save(string path, DataTable dt)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Create(path)))
            {
                bw.Write(1); // version
                var ext = GetObjectType(path);
                bool useLevel = UsesLevelAndPointer(ext);
                WriteTable(bw, dt, "Original", useLevel);
                WriteTable(bw, dt, "Custom", useLevel);
            }
        }

        private static void WriteTable(BinaryWriter bw, DataTable dt, string source, bool useLevel)
        {
            var rows = dt.AsEnumerable()
                         .Where(r => r["Source"].ToString() == source)
                         .ToList();

            var grouped = rows.GroupBy(r => new
            {
                OriginalID = r["OriginalID"].ToString(),
                NewID = r["NewID"].ToString()
            });

            bw.Write(grouped.Count());

            foreach (var objectGroup in grouped)
            {
                string origId = objectGroup.Key.OriginalID;
                string newId = objectGroup.Key.NewID;
                if (source == "Original")
                {
                    newId = origId;
                }

                bw.Write(Encoding.ASCII.GetBytes(Util.Fix4(origId)));
                bw.Write(Encoding.ASCII.GetBytes(Util.Fix4(newId)));

                bw.Write(objectGroup.Count());

                foreach (var row in objectGroup)
                {
                    string fieldId = row["FieldID"].ToString();
                    string typeStr = row["Type"].ToString();
                    string valueStr = row["Value"].ToString();

                    int valueType = Util.ParseTypeCode(typeStr);
                    int level = dt.Columns.Contains("Level") && row["Level"] != DBNull.Value ? Convert.ToInt32(row["Level"]) : 0;
                    int dataPointer = dt.Columns.Contains("DataPointer") && row["DataPointer"] != DBNull.Value ? Convert.ToInt32(row["DataPointer"]) : 0;

                    bw.Write(Encoding.ASCII.GetBytes(Util.Fix4(fieldId)));
                    bw.Write(valueType);

                    if (useLevel)
                    {
                        bw.Write(level);
                        bw.Write(dataPointer);
                    }

                    switch (valueType)
                    {
                        case 0:
                            int intVal;
                            int.TryParse(valueStr, out intVal);
                            bw.Write(intVal);
                            break;
                        case 1:
                        case 2:
                            float floatVal;
                            float.TryParse(valueStr, out floatVal);
                            bw.Write(floatVal);
                            break;
                        case 3:
                            Util.WriteNullTerminatedString(bw, valueStr);
                            break;
                        default:
                            bw.Write(0);
                            break;
                    }

                    bw.Write(0); // End marker
                }
            }
        }

        private static void ReadTable(BinaryReader br, string source, DataTable dt, bool useLevel)
        {
            if (br.BaseStream.Position + 4 > br.BaseStream.Length) return;
            int objectCount = br.ReadInt32();

            for (int i = 0; i < objectCount; i++)
            {
                if (br.BaseStream.Position + 12 > br.BaseStream.Length) break;
                string originalId = Encoding.ASCII.GetString(br.ReadBytes(4));
                string newId = Encoding.ASCII.GetString(br.ReadBytes(4));
                int modCount = br.ReadInt32();

                for (int j = 0; j < modCount; j++)
                {
                    if (br.BaseStream.Position + 8 > br.BaseStream.Length) break;
                    string fieldId = Encoding.ASCII.GetString(br.ReadBytes(4));
                    int valueType = br.ReadInt32();

                    int level = 0;
                    int dataPointer = 0;
                    if (useLevel)
                    {
                        if (br.BaseStream.Position + 8 > br.BaseStream.Length) break;
                        level = br.ReadInt32();
                        dataPointer = br.ReadInt32();
                    }

                    object value = null;

                    try
                    {
                        switch (valueType)
                        {
                            case 0:
                                value = br.ReadInt32();
                                break;
                            case 1:
                            case 2:
                                value = br.ReadSingle();
                                break;
                            case 3:
                                value = Util.ReadNullTerminatedString(br);
                                break;
                            default:
                                if (valueType > 100)
                                    value = br.ReadInt32();
                                else
                                    value = "[type:" + valueType + "]";
                                break;
                        }
                    }
                    catch
                    {
                        value = "[invalid]";
                    }

                    if (br.BaseStream.Position + 4 <= br.BaseStream.Length)
                        br.ReadInt32();
                    else
                        break;

                    dt.Rows.Add(source,
                        originalId,
                        source == "Original" ? "(base)" : newId,
                        fieldId,
                        TypeName(valueType),
                        value?.ToString() ?? "",
                        level,
                        dataPointer);
                }
            }
        }

        private static string TypeName(int type)
        {
            switch (type)
            {
                case 0: return "int";
                case 1: return "real";
                case 2: return "unreal";
                case 3: return "string";
                default: return $"type{type}";
            }
        }

        internal static DataTable CreateEmptyDataTable()
        {
            return CreateDataTable();
        }

        private static DataTable CreateDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Source");
            dt.Columns.Add("OriginalID");
            dt.Columns.Add("NewID");
            dt.Columns.Add("FieldID");
            dt.Columns.Add("Type");
            dt.Columns.Add("Value");
            dt.Columns.Add("Level", typeof(int));         // 추가
            dt.Columns.Add("DataPointer", typeof(int));   // 추가
            return dt;
        }
    }

    public static class W3ObjectCsvHandler
    {
        public static DataTable Load(string path)
        {
            var dt = W3ObjectFileHandler.CreateEmptyDataTable();

            using (var sr = new StreamReader(path, Encoding.UTF8))
            {
                string header = sr.ReadLine();
                if (header == null)
                {
                    return dt;
                }

                var currentLine = new StringBuilder();
                string rawLine;
                while ((rawLine = sr.ReadLine()) != null)
                {
                    currentLine.AppendLine(rawLine);
                    string line = currentLine.ToString();

                    int quoteCount = line.Count(c => c == '"');
                    if (quoteCount % 2 != 0)
                    {
                        continue;
                    }

                    string[] parts = ParseCsvLine(line.TrimEnd('\r', '\n'));
                    currentLine.Clear();

                    if (parts.Length < 6)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[4]))
                    {
                        continue;
                    }

                    var newRow = dt.NewRow();
                    newRow["Source"] = parts[0];
                    newRow["OriginalID"] = parts[1];
                    newRow["NewID"] = parts[2];
                    newRow["FieldID"] = parts[3];
                    newRow["Type"] = parts[4];
                    newRow["Value"] = string.IsNullOrWhiteSpace(parts[5]) ? "" : parts[5];

                    if (parts.Length > 6 && int.TryParse(parts[6], out int level))
                        newRow["Level"] = level;
                    else
                        newRow["Level"] = 0;

                    if (parts.Length > 7 && int.TryParse(parts[7], out int dataPointer))
                        newRow["DataPointer"] = dataPointer;
                    else
                        newRow["DataPointer"] = 0;

                    dt.Rows.Add(newRow);
                }
            }

            return dt;
        }

        public static void Save(string path, DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            bool includeExtraColumns = dt.Columns.Contains("Level") && dt.Columns.Contains("DataPointer");

            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                var headerCols = new List<string> { "Source", "OriginalID", "NewID", "FieldID", "Type", "Value" };
                if (includeExtraColumns)
                {
                    headerCols.Add("Level");
                    headerCols.Add("DataPointer");
                }

                sw.WriteLine(string.Join(",", headerCols));

                foreach (DataRow row in dt.Rows)
                {
                    var fields = new List<string>
                    {
                        EscapeCsv(row["Source"]),
                        EscapeCsv(row["OriginalID"]),
                        EscapeCsv(row["NewID"]),
                        EscapeCsv(row["FieldID"]),
                        EscapeCsv(row["Type"]),
                        EscapeCsv(row["Value"]?.ToString().Replace("\n", "\\n"))
                    };

                    if (includeExtraColumns)
                    {
                        fields.Add(EscapeCsv(row["Level"]));
                        fields.Add(EscapeCsv(row["DataPointer"]));
                    }

                    sw.WriteLine(string.Join(",", fields));
                }
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }

        private static string EscapeCsv(object value)
        {
            if (value == null || value == DBNull.Value) return "";

            string str = value.ToString();

            if (str.Contains(",") || str.Contains("\"") || str.Contains("\n") || str.Contains("\r"))
            {
                return "\"" + str.Replace("\"", "\"\"") + "\"";
            }

            return str;
        }
    }
}
