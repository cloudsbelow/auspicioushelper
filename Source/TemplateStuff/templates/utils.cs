


namespace Celeste.Mod.auspicioushelper;

public static class TemplateHelpers{
  public static void EnableSms(this Platform plat, int vis){
    if(plat.staticMovers.Count==0) return;
    if(vis==1) plat.EnableStaticMovers();
    else {
      foreach(var sm in plat.staticMovers){
        bool orig = sm.Entity.Visible;
        sm.Enable();
        if(vis==-1) sm.Entity.Visible = false;
        else if (vis == 0) sm.Entity.Visible = orig;
      }
    }
  }
  public static void DisableSms(this Platform plat, int vis){
    if(plat.staticMovers.Count==0) return;
    if(vis==-1) plat.DisableStaticMovers();
    else {
      foreach(var sm in plat.staticMovers){
        bool orig = sm.Entity.Visible;
        sm.Enable();
        if(vis==1) sm.Entity.Visible = true;
        else if (vis == 0) sm.Entity.Visible = orig;
      }
    }
  }
}