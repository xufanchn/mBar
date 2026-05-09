using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace mBar.src.SystemServices
{
    /// <summary>
    /// 硬件扫描器：负责硬件的发现、列举和命名逻辑 (主要服务于 UI 设置)
    /// </summary>
    public static class HardwareScanner
    {
        private static List<string>? _cachedFanList = null;
        private static List<string>? _cachedNetworkList = null;
        private static List<string>? _cachedDiskList = null;
        private static List<string>? _cachedMoboTempList = null;

        /// <summary>
        /// 清除所有扫描缓存
        /// </summary>
        public static void ClearCache()
        {
            _cachedFanList = null;
            _cachedNetworkList = null;
            _cachedDiskList = null;
            _cachedMoboTempList = null;
        }

        /// <summary>
        /// 智能命名：将传感器与其所属硬件结合命名，并处理 SuperIO 芯片名的替换
        /// </summary>
        public static string GenerateSmartName(ISensor sensor, IHardware hardware, IComputer computer)
        {
            string hwName = hardware.Name;
            // 如果是 SuperIO，尝试替换为主板名
            if (hardware.HardwareType == HardwareType.SuperIO)
            {
                var mobo = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard);
                if (mobo != null) hwName = mobo.Name;
            }
            return $"{sensor.Name} [{hwName}]";
        }

        /// <summary>
        /// 列出所有网卡名称
        /// </summary>
        public static List<string> ListAllNetworks(IComputer computer)
        {
            if (_cachedNetworkList != null && _cachedNetworkList.Count > 0)
                return _cachedNetworkList.ToList();

            var list = computer.Hardware
                .Where(h => h.HardwareType == HardwareType.Network)
                .Select(h => h.Name).Distinct().ToList();

            if (list.Count > 0) _cachedNetworkList = list;
            return list.ToList();
        }

        /// <summary>
        /// 列出所有硬盘名称
        /// </summary>
        public static List<string> ListAllDisks(IComputer computer)
        {
            if (_cachedDiskList != null && _cachedDiskList.Count > 0)
                return _cachedDiskList.ToList();

            var list = computer.Hardware
                .Where(h => h.HardwareType == HardwareType.Storage)
                .Select(h => h.Name).Distinct().ToList();

            if (list.Count > 0) _cachedDiskList = list;
            return list.ToList();
        }

        /// <summary>
        /// 列出所有风扇传感器 (排除 CPU/GPU 等核心自带风扇，主要针对主板/机箱风扇)
        /// </summary>
        public static List<string> ListAllFans(IComputer computer, object syncLock)
        {
            if (_cachedFanList != null && _cachedFanList.Count > 0)
                return _cachedFanList.ToList();

            var list = new List<string>();
            lock (syncLock)
            {
                void Scan(IHardware hw)
                {
                    bool isExcluded = hw.HardwareType == HardwareType.GpuNvidia || hw.HardwareType == HardwareType.GpuAmd ||
                                      hw.HardwareType == HardwareType.GpuIntel || hw.HardwareType == HardwareType.Cpu ||
                                      hw.HardwareType == HardwareType.Storage || hw.HardwareType == HardwareType.Memory ||
                                      hw.HardwareType == HardwareType.Network;

                    if (!isExcluded)
                    {
                        foreach (var s in hw.Sensors)
                        {
                            if (s.SensorType == SensorType.Fan) 
                                list.Add(GenerateSmartName(s, hw, computer));
                        }
                    }
                    foreach (var sub in hw.SubHardware) Scan(sub);
                }
                foreach (var hw in computer.Hardware) Scan(hw);
            }

            list.Sort();
            var final = list.Distinct().ToList();
            if (final.Count > 0) _cachedFanList = final;
            return final.ToList();
        }

        /// <summary>
        /// 列出所有适合作为主板/系统温度的传感器
        /// </summary>
        public static List<string> ListAllMoboTemps(IComputer computer, object syncLock)
        {
            if (_cachedMoboTempList != null && _cachedMoboTempList.Count > 0)
                return _cachedMoboTempList.ToList();

            var list = new List<string>();
            lock (syncLock)
            {
                void Scan(IHardware hw)
                {
                    bool isExcluded = hw.HardwareType == HardwareType.Cpu || hw.HardwareType == HardwareType.GpuNvidia ||
                                      hw.HardwareType == HardwareType.GpuAmd || hw.HardwareType == HardwareType.GpuIntel ||
                                      hw.HardwareType == HardwareType.Storage || hw.HardwareType == HardwareType.Memory ||
                                      hw.HardwareType == HardwareType.Network;

                    if (!isExcluded)
                    {
                        foreach (var s in hw.Sensors)
                        {
                            if (s.SensorType == SensorType.Temperature) 
                                list.Add(GenerateSmartName(s, hw, computer));
                        }
                    }
                    foreach (var sub in hw.SubHardware) Scan(sub);
                }
                foreach (var hw in computer.Hardware) Scan(hw);
            }

            list.Sort();
            var final = list.Distinct().ToList();
            if (final.Count > 0) _cachedMoboTempList = final;
            return final.ToList();
        }
    }
}
