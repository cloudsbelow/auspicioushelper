


using System;
using System.Collections.Generic;

namespace Celeste.Mod.auspicioushelper.channelmath;

public static class Irl{
  public static void Register(){
    ChannelMathController.registerInterop("time",(strs,i)=>strs.Count<=1?
      (int)(DateTime.Now-new DateTime(DateTime.Now.Year,1,1)).TotalSeconds:strs[1] switch{
      "Year" or "Yr" or "Y"=>DateTime.Now.Year,
      "Month" or "Mo" or "M"=>DateTime.Now.Month,
      "Day" or "D"=>DateTime.Now.Day,
      "DayOfWeek" or "DoW"=>(int)DateTime.Now.DayOfWeek,
      "DayOfYear" or "DoY"=>DateTime.Now.DayOfYear,
      "Hours" or "h"=>DateTime.Now.Hour,
      "Minutes" or "Min" or "m"=>DateTime.Now.Minute,
      "Seconds" or "Sec" or "s"=>DateTime.Now.Second,
      "Milliseconds" or "Ms" or "ms"=>DateTime.Now.Millisecond,
      _=>0
    });
  }
}