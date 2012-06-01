using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

/*类名 :         SFIO
 *目的：        小文件简单打包
 *设计思想： 小文件通过定长HASH字符串或者序号进行访问
 *                 HASH对应包文件的Offset
 */

namespace Sunfish.IO
{
    public class SFIO
    {
        public SFIO()
        {
            files.Columns.Clear();
            files.Columns.Add(new DataColumn("seq", System.Type.GetType("System.Int64")));
            files.Columns.Add(new DataColumn("size", System.Type.GetType("System.Int64")));
            files.Columns.Add(new DataColumn("offset", System.Type.GetType("System.Int64")));
            files.Columns.Add(new DataColumn("fn", System.Type.GetType("System.String")));
        }

        public readonly string header = "header description";
        public readonly string k = "This is a Sunfish SFIO BagFile and  filename,offset,size will be in .idx file as a plain text";

        public class SFile
        {
            public string fn;
            public Int32 size;
            public Int64 offset;
            public Int64 seq;

            /// <summary>
            /// 初始化
            /// </summary>
            /// <param name="s">大小</param>
            /// <param name="se">序列</param>
            /// <param name="of">偏移</param>
            /// <param name="filename">32位hash名字</param>
            public SFile(Int32 s, Int64 se, Int64 of, string filename)
            {
                size = s;
                fn = filename;
                seq = se;
                offset = of;
            }
        }

        Dictionary<string, SFile> fileInfo = new Dictionary<string, SFile>();
        /// <summary>最大文件编码</summary>
        public Int64 maxSeq = 0;

        /// <summary> 最大文件偏移量</summary>
        public Int64 maxOffset = 0;

        /// <summary>打包后文件体</summary>
        private FileStream bagFile;
        private string baseFileName;
        /// <summary>索引文件体</summary>
        private FileStream indexFile;
        private DataTable files = new DataTable();

        public enum SFIOStatus
        {
            Debug = -1,
            OK = 0,
            FileError = 1,
            FileNotFound = 2,
            FileSizeWrong = 3,
            IndexNotFound = 4
        }

        /// <summary> 创建SFIO</summary>
        /// <param name="pathToSFIO">指定SFIO文件夹路径</param>
        public SFIOStatus initSFIO(string pathToSFIO)
        {
            baseFileName = pathToSFIO;

            bagFile = new FileStream(baseFileName + ".bag", FileMode.Create);
            indexFile = new FileStream(baseFileName + ".idx", FileMode.Create);
            indexFile.Flush();
            indexFile.Dispose();
            maxSeq = 0;

            StreamWriter sw = new StreamWriter(bagFile);
            //sw.Write();
            DataRow dr = files.NewRow();

            dr[0] = maxSeq++;

            dr[1] = k.Length;

            dr[2] = maxOffset;

            maxOffset += Convert.ToInt64(k.Length);

            dr[3] = header;

            files.Rows.Add(dr);
            sw.Write(k);
            sw.Flush();
            writeIndex();
            return SFIOStatus.Debug;
        }

        /// <summary>
        /// writeIndex() 保存索引流，保存文件流
        /// </summary>
        /// <returns></returns>
        public SFIOStatus writeIndex()
        {
            //baseFileName = pathToSFIO;
            indexFile.Close();
            indexFile = new FileStream(baseFileName + ".idx", FileMode.Create);

            StreamWriter sw = new StreamWriter(indexFile);
            SFile info = fileInfo[header];
            info.offset = maxOffset;
            info.seq = maxSeq;
            info.size = k.Length;

            foreach (SFile item in fileInfo.Values)
            {
                sw.WriteLine(item.seq.ToString() + "@" + item.size.ToString() + "@" + item.offset.ToString() + "@" + item.fn);
                // sw.WriteLine(item[0].ToString() + "@" + item[1].ToString() + "@" + item[2].ToString() + "@" + item[3].ToString());
            }
            sw.Flush();
            return SFIOStatus.OK;
        }

        /// <summary>
        /// 加载SFIO
        /// </summary>
        /// <param name="pathToSFIO">文件名</param>
        /// <returns></returns>
        public SFIOStatus loadSFIO(string pathToSFIO)
        {
            //string t = "";
            //DataRow dr;
            string[] k = { "" };
            if (File.Exists(pathToSFIO + ".idx"))
            {
                baseFileName = pathToSFIO;
                if (bagFile != null) bagFile.Close();
                if (indexFile != null) indexFile.Close();

                bagFile = File.Open(pathToSFIO + ".bag", FileMode.Open, FileAccess.ReadWrite);
                indexFile = File.Open(pathToSFIO + ".idx", FileMode.Open, FileAccess.ReadWrite);
                indexFile.Position = 0;
                fileInfo.Clear();
                using (StreamReader sw = new StreamReader(indexFile))
                {
                    while (!sw.EndOfStream)
                    {
                        k = sw.ReadLine().Split('@');
                        fileInfo.Add(k[3], new SFile(Convert.ToInt32(k[1]), Convert.ToInt64(k[0]), Convert.ToInt64(k[2]), k[3]));
                    }

                    SFile info = fileInfo[header];
                    //info.offset = maxOffset;
                    //info.seq = maxSeq;
                    maxSeq = info.seq;
                    maxOffset = info.offset;
                    if (maxSeq == 0)
                    {
                        info.seq = Convert.ToInt64(k[0]);
                        info.offset = Convert.ToInt64(k[1]) + Convert.ToInt64(k[2]);
                    }
                    maxSeq = info.seq;
                    maxOffset = info.offset;
                }
                //indexFile.Dispose();
                return SFIOStatus.OK;
            }
            else
            {
                return SFIOStatus.IndexNotFound;
            }
        }

        /// <summary> 获取文件</summary>
        /// <param name="fn">文件名字串</param>
        /// <returns>文件内存流</returns>
        public MemoryStream getFile(string fn)
        {
            SFile m;
            if (fileInfo.ContainsKey(fn))
            {
                m = fileInfo[fn];
                MemoryStream ms = new MemoryStream();
                int size; Int64 offset;
                offset = Convert.ToInt64(m.offset);
                size = Convert.ToInt32(m.size);
                byte[] kx = new byte[size];
                bagFile.Position = offset;
                bagFile.Read(kx, 0, size);
                ms.Position = 0;
                ms.Write(kx, 0, size);
                ms.Flush();
                return ms;
            }
            else return null;
        }

        public byte[] getFileB(string fn)
        {
            SFile m;
            if (fileInfo.ContainsKey(fn))
            {
                m = fileInfo[fn];
                MemoryStream ms = new MemoryStream();
                int size; Int64 offset;
                offset = Convert.ToInt64(m.offset);
                size = Convert.ToInt32(m.size);
                byte[] kx = new byte[size];
                bagFile.Read(kx, 0, size);
                return kx;
            }
            else return null;
        }

        /// <summary>根据序号返回文件</summary>
        /// <param name="fn">序号</param>
        /// <returns>文件内存流</returns>
        public MemoryStream getFile(UInt64 fn)
        {
            return null;
        }

        /// <summary>存入文件</summary>
        /// <param name="fn">文件名</param>
        /// <returns>存入结果 </returns>
        public SFIOStatus putFile_(string[] fn)
        {
            byte[] fi;
            string re = "V:\\Thumbs\\{0}\\{1}\\{2}";
            string fx;
            //StreamWriter sw = new StreamWriter(bagFile);
            foreach (string item in fn)
            {
                //打开文件流 00a0c d56af 12977 2d2b0 f1f1b d69e0 8b.jpg
                if (item.Length > 32)
                {
                    fx = string.Format(re, item.Substring(0, 1), item.Substring(1, 1), item);
                    if (File.Exists(fx))
                    {
                        fi = File.ReadAllBytes(fx);
                        fileInfo.Add(item.Substring(0, 32), new SFile(
                            fi.Length,
                            maxSeq++,
                            maxOffset,
                            item.Substring(0, 32)

                            ));

                        DataRow dr = files.NewRow();
                        dr[0] = maxSeq++;
                        dr[1] = fi.Length;

                        dr[2] = maxOffset;

                        dr[3] = item.Substring(0, 32);
                        files.Rows.Add(dr);
                        bagFile.Position = maxOffset;
                        bagFile.Write(fi, 0, fi.Length);
                        maxOffset += Convert.ToInt64(fi.Length);
                    }
                }
            }
            bagFile.Flush();
            writeIndex();
            return SFIOStatus.OK;
        }

        public SFIOStatus putFile_2(string[] fn)
        {
            byte[] fi;
            //string re = "V:\\Thumbs\\{0}\\{1}\\{2}";
            string fx = "", fnx = "";
            //StreamWriter sw = new StreamWriter(bagFile);
            foreach (string item in fn)
            {
                //打开文件流 00a0c d56af 12977 2d2b0 f1f1b d69e0 8b.jpg
                if (item.Length > 32)
                {
                    //fx = string.Format(re, item.Substring(0, 1), item.Substring(1, 1), item);
                    fx = item;
                    if (fx.Length < 36) break;
                    fnx = item.Substring(fx.Length - 36, 32);
                    if (File.Exists(fx))
                    {
                        fi = File.ReadAllBytes(fx);

                        if (!fileInfo.ContainsKey(fnx))
                        {
                            fileInfo.Add(fnx, new SFile(
                                fi.Length,
                                maxSeq++,
                                maxOffset,
                                fnx

                                ));

                            // DataRow dr = files.NewRow();
                            //dr[0] = maxSeq++;
                            //dr[1] = fi.Length;

                            //dr[2] = maxOffset;

                            // dr[3] = fn;
                            //   files.Rows.Add(dr);
                            bagFile.Position = maxOffset;
                            bagFile.Write(fi, 0, fi.Length);
                            maxOffset += Convert.ToInt64(fi.Length);
                        }
                    }
                }
            }
            bagFile.Flush();
            writeIndex();
            return SFIOStatus.OK;
        }

        /// <summary>根据编码生成补丁文件</summary>
        /// <param name="sn">补丁编码</param>
        /// <param name="pathToPatch">补丁文件路径</param>
        /// <returns>保存状态</returns>
        public SFIOStatus createPatch(UInt64 sn, string pathToPatch)
        {
            return SFIOStatus.Debug;
        }

        /// <summary>向包中追加文件补丁</summary>
        /// <param name="pathToPatch">补丁路径</param>
        /// <returns>追加结果</returns>
        public SFIOStatus applyPatch(string pathToPatch)
        {
            return SFIOStatus.Debug;
        }
    }
}