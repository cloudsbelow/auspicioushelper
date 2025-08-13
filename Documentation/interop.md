Auspicious heavily recommends using ModInterop and not assembly references for interop. Any API in the 'Source/iop' folder will remain supported. Any other files and namespaces are liable (and likely!) to change. We encourage using Auspicious as just an optional dependency rather than a required one. 

## Template interop
Auspicioushelper offers an easy way to fix your entities entities when they're put in templates. The main way to do this is the interop function
`void customClarify(string, Func<Level, LevelData, Vector2, EntityData, Component>);`
This should be almost identical to a conventional Level.EntityLoader that returns a component on the entity rather than the entity itself. The component used for this should match the signature expected by Auspicious for getting callbacks. A base class extending Component is provided in the template ModImport. It is recommended to extend this component for any usage with this function to ensure all expected fields are present. Auspicious will handle parsing this class and adding all callbacks. The full signature for this provided component is 
`
public class TemplateChildComponent:Component {
    public TemplateChildComponent(Entity ent)

    Entity parent = null;
    public Action<Scene> AddTo = null;
    public Action<List<Entity>> AddSelf = null;
    public Action<Vector2, Vector2> RepositionCB = null;
    public Action<Vector2> SetOffsetCB = null;

    public Action<int,int,int> ChangeStatusCB = null; 
    public bool ParentVisible = true;
    public bool ParentCollidable = true;
    public bool ParentActive = true; 
    public Action<bool> DestroyCB = null;

    public void TriggerParent()=>triggerTemplate(parent, Entity);
    public DashCollisionResults RegisterDashhit(Player p, Vector2 dir)=>registerDashhit(parent, p, dir);
    public void AddPlatform()=>registerPlatform(parent, Entity);
    public Vector2 getParentLiftspeed()=>getTemplateLiftspeed(parent);
  }
` 
The component you return should have non-null fields wherever the default behavior from auspicious does not suffice. Let's cover each set of functions.
Firstly, RepositionCB and SetOffsetCB. You'll want to use these whenever your entity moves with absolute rather than relative positioning to make sure your entity works in moving templates. SetOffsetCB is called shortly after the component is added to the template with the template's virtual location (at which the origin of its filler is placed; you should not directly use parent.Position because the origin and Position of many template entities does not match). You should use this to store the offset of your entity in the template with this call and correspondingly adjust any absolute anchors on the Reposition callbacks. When you move your own solids in templates, you should make sure that you are adding the liftspeed of the parents to the liftspeed of your object (and vice versa). getParentLiftspeed can be used for this.

Next, let's cover ChangeStatusCB and DestroyCB. These are fairly simple; Destroy is called when the template is removed. Its boolean argument indicates whether particles and debris can be produced (you do not need to make particles when the argument is true but you should definitely avoid making them when it is false.) ChangeStatusCB is called whenever the template's visibility, collidability or active status changes. '1' means that the corresponding field is being set to true, '-1' means setting to false, and '0' means this field is not being changed. You can directly read the current status of the parent template with the provided ParentXYZ fields. You would want to write a custom ChangeStatusCB if your entity often changed its own status, for instance in the case of a custom refill which becomes uncollidable when a player hits it and turns itself collidable again after the respawn time. In this case, you would write a ChangeStatusCB that ensures that your refill is only collidable and visible if it is not currently respawning and if its parent is also collidable/visible.

Finally, let's cover AddTo and AddSelf. AddTo is like a surrogate for Added and is called immediately after the entity has been put into a template. AddSelf should add the entity and any of its children to a list. You would write custom functions for these if your entity spawned many children who all need management. For example, let's suppose you have a Gate entity with a separate Solid for either side. You want to ensure that AddSelf adds not only the main Gate entity but also both children. For maximum compatibility, you should ensure that as soon as your AddTo returns, you are ready to add *all* these entities in an immediately following AddSelf call. Note that both of these functions may be called before your entity's main added function is called! Finally, if your entity constructs platforms later, you should call RegisterPlatform on them, which ensures that collision and touch events are properly sent to the parent templates. This registration is automatically performed on platforms present in AddSelf immediately after the call to AddTo.

In summary, the default behavior of auspicious entities is fairly good, but if you ever have difficult to account for behavior in a certain 'domain' you should write callbacks to account for it.
 - If your entity moves with absolute anchors, you will want to write SetOffset and Reposition callbacks
 - If your entity frequently changes its visibility or collidability, you will want to write a ChangeStatus callbacks
 - If your entity creates child entities that should be accounted for you should write AddTo and AddSelf callbacks

Finally, if you ever want to 'trigger' a template, you can use the provided function. You should also call the RegisterDashHit on dashHits to platforms in your entity if there is not conflicting behavior.

