using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Seengene.XDK
{
    
    [System.Serializable]
    public class XDKMapItem 
    {
        private static int Sunrise_Summer = 445; // 夏至日出时间
        private static int Sunset_Summer = 1947; // 夏至日落时间
        private static int Sunrise_Winter = 736; // 冬至日出时间
        private static int Sunset_Winter = 1649; // 冬至日落时间

        public long MapIdDaytime;

        /// <summary>
        /// 是否使用2张地图
        /// </summary>
        public bool UseTwoMaps;

        public long MapIdNight;

        public Vector3 OffsetPos;

        public Vector3 OffsetEuler;


        public bool UseAutoDaytime;
        /// <summary>
        /// 白天开始的时间，610表示上午 6:10
        /// </summary>
        public int DaytimeStart = 610;

        /// <summary>
        /// 夜晚开始的时间，1720表示下午 17:20
        /// </summary>
        public int NightStart = 1720;


        /// <summary>
        /// 获取当前的mapID
        /// </summary>
        /// <param name="byEditor"></param>
        /// <returns></returns>
        public long GetMapIdNow()
        {
            if (UseTwoMaps)
            {
                if (MapIdNight <= 0)
                {
                    return MapIdDaytime;
                }
                else
                {
                    bool isNight = IsNightTime(System.DateTime.Now);
                    if (isNight)
                    {
                        return MapIdNight;
                    }
                    else
                    {
                        return MapIdDaytime;
                    }
                }
            }
            else
            {
                return MapIdDaytime;
            }
        }


        /// <summary>
        /// 是否已经是晚上
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public bool IsNightTime(System.DateTime dt)
        {
            int sunrise, sunset;
            if (UseAutoDaytime) // 自动获取日出日落时间
            {
                GetDaytime(dt, out sunrise, out sunset);
            }
            else
            {
                sunrise = DaytimeStart;
                sunset = NightStart;
            }


            int v1 = dt.Hour * 100 + dt.Minute;
            if (v1 >= sunset)
            {
                return true;
            }
            if (v1 < sunrise)
            {
                return true;
            }
            return false;
        }

        public static void TestAllYearTime()
        {
            int sunrise, sunset;
            for (int i = 0; i < 12; i++)
            {
                for (int k = 0; k < 30; k++)
                {
                    if(i == 1 && k > 27)
                    {
                        continue;
                    }
                    var today = new System.DateTime(System.DateTime.Now.Year, i + 1, k + 1);
                    GetDaytime(today, out sunrise, out sunset);
                    Debug.Log("--> " + today.Month + "月 " + today.Day + "日 sunrise=" + sunrise + " sunset=" + sunset);
                }
            }
        }


        /// <summary>
        /// 计算日出日落时间
        /// </summary>
        /// <param name="sunrise"></param>
        /// <param name="sunset"></param>
        private static void GetDaytime(System.DateTime today, out int sunrise, out int sunset)
        {
            var SummerSolstice = new System.DateTime(today.Year, 6, 21); // 夏至 
            var WinterSolstice = new System.DateTime(today.Year, 12, 22); // 冬至

            float sunriseSum = SixtyToHundred(Sunrise_Summer);
            float sunsetSum = SixtyToHundred(Sunset_Summer);
            float sunriseWin = SixtyToHundred(Sunrise_Winter);
            float sunsetWin = SixtyToHundred(Sunset_Winter);
            int _sunrise, _sunset;
            if (today.DayOfYear >= WinterSolstice.DayOfYear) // 已经过了冬至
            {
                var passed = today - WinterSolstice;
                double ratio = passed.TotalDays / 183.03d;
                _sunrise = (int)(sunriseWin - (sunriseWin - sunriseSum) * ratio);
                _sunset = (int)(sunsetWin + (sunsetSum - sunsetWin) * ratio);
            }
            else if (today.DayOfYear <= SummerSolstice.DayOfYear) // 还没到夏至
            {
                var toSummer = SummerSolstice - today;
                double ratio = 1 - toSummer.TotalDays / 183.03d;
                _sunrise = (int)(sunriseWin - (sunriseWin - sunriseSum) * ratio);
                _sunset = (int)(sunsetWin + (sunsetSum - sunsetWin) * ratio);
            }
            else // 过了夏至，还没到冬至之间
            {
                var passed = today - SummerSolstice;
                double ratio = passed.TotalDays / 183.03d;
                _sunrise = (int)(sunriseSum + (sunriseWin - sunriseSum) * ratio);
                _sunset = (int)(sunsetSum - (sunsetSum - sunsetWin) * ratio);
            }
            sunrise = HundredToSixty(_sunrise);
            sunset = HundredToSixty(_sunset);
        }

        /// <summary>
        /// 让分钟数从60 转换到 100，避免将来对int插值时的空隙
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static int SixtyToHundred(int val)
        {
            int hh = val / 100;
            int mm = val % 100;
            return hh * 100 + (int)(mm * 1.0f / 60 * 100);
        }

        /// <summary>
        /// 在插值之后，将分钟数从100 转换到 60
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static int HundredToSixty(int val)
        {
            int hh = val / 100;
            int mm = val % 100;
            return hh * 100 + (int)(mm * 1.0f / 100 * 60);
        }

        /// <summary>
        /// 获得本对象的文本描述
        /// </summary>
        /// <returns></returns>
        public new string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("MapIdDaytime=" + MapIdDaytime);
            sb.Append(" UseTwoMaps=" + UseTwoMaps);
            sb.Append(" MapIdNight=" + MapIdNight);
            sb.Append(" OffsetPos=" + OffsetPos.ToString("f2"));
            sb.Append(" OffsetEuler=" + OffsetEuler.ToString("f2"));
            sb.Append(" UseAutoDaytime=" + UseAutoDaytime);
            if (UseAutoDaytime)
            {
                GetDaytime(System.DateTime.Now, out int sunrise, out int sunset);
                sb.Append(" DaytimeStart=" + sunrise);
                sb.Append(" NightStart=" + sunset);
            }
            else
            {
                sb.Append(" DaytimeStart=" + DaytimeStart);
                sb.Append(" NightStart=" + NightStart);
            }

            return sb.ToString();
        }
    }
}
