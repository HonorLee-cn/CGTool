/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-04-15)
 * @License GPL-3.0
 *
 * Palet.cs 调色板解析类
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CrossgateToolkit
{
    public class Palet
    {

        private static Dictionary<int, List<Color32>> _cache = new Dictionary<int, List<Color32>>();

        //获取调色板
        public static List<Color32> GetPalet(int index)
        {
            //返回缓存数据
            _cache.TryGetValue(index, out List<Color32> paletData);
            return paletData;
        }

        //调色板初始化
        public static void Init()
        {
            DirectoryInfo folderInfo = new DirectoryInfo(CGTool.PATH.PAL);

            if (!folderInfo.Exists) throw new Exception("调色板目录不存在,请检查CGTool中是否配置相应PATH路径");
            
            FileInfo[] files = folderInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                if (!file.Name.StartsWith("palet_") || !file.Name.EndsWith(".cgp")) continue;
                string tmp = file.Name.Split("_")[1];
                string indexStr = tmp.Split(".")[0];
                // string indexStr = file.Name.Substring(6, 2);
                if (int.TryParse(indexStr, out int index))
                {
                    List<Color32> paletData = _loadPalet(file);
                    if (paletData != null) _cache.Add(index, paletData);    
                }
            }

            Debug.Log("[CGTool] 调色板初始化完成,共加载" + _cache.Count + "个调色板");
        }
        
        // 添加新调色板
        public static void AddPalet(int index, List<Color32> paletData)
        {
            _cache[index] = paletData;
        }
        
        //加载缓存数据
        private static List<Color32> _loadPalet(FileInfo paletFile)
        {
            FileStream paletFileStream = paletFile.OpenRead();
            BinaryReader paletReader = new BinaryReader(paletFileStream);
            
            //调色板解析表
            List<Color32> PaletColors = new List<Color32>();

            //头部调色板
            int[] headPlate = new int[]
            {
                0x000000, 0x000080, 0x008000, 0x008080, 0x800080, 0x800000, 0x808000, 0xc0c0c0, 0xc0dcc0, 0xf0caa6,
                0x0000de, 0x005fff, 0xa0ffff, 0xd25f00, 0xffd250, 0x28e128
            };
            //尾部调色板
            int[] footPlate = new int[]
            {
                0x96c3f5, 0x5fa01e, 0x467dc3, 0x1e559b, 0x374146, 0x1e2328, 0xf0fbff, 0xa56e3a, 0x808080, 0x0000ff,
                0x00ff00, 0x00ffff, 0xff0000, 0xff80ff, 0xffff00, 0xffffff
            };

            //解压缩
            foreach (var i in headPlate)
            {
                byte[] bytes = BitConverter.GetBytes(i);
                Color32 color32 = new Color32();
                color32.r = bytes[0];
                color32.g = bytes[1];
                color32.b = bytes[2];
                color32.a = (byte)(i == 0 ? 0x00 : 0xFF);
                PaletColors.Add(color32);
            }

            
            
            
            for (var i = 0; i < 224; i++)
            {
                byte[] paletBytes = paletReader.ReadBytes(3);
                // if(i<16 || i>224) continue;
                Color32 color32 = new Color32();
                color32.r = paletBytes[2];
                color32.g = paletBytes[1];
                color32.b = paletBytes[0];
                color32.a = 0xFF;
                PaletColors.Add(color32);
            }
            
            foreach (var i in footPlate)
            {
                byte[] bytes = BitConverter.GetBytes(i);
                Color32 color32 = new Color32();
                color32.r = bytes[0];
                color32.g = bytes[1];
                color32.b = bytes[2];
                color32.a = 0xFF;
                PaletColors.Add(color32);
            }
            
            PaletColors.Add(Color.clear);
            //清理缓存
            paletReader.Dispose();
            paletReader.Close();
            paletFileStream.Dispose();
            paletFileStream.Close();

            return PaletColors;
        }
    }
}