## Todo

最终Boss

KingSlime能对敌对生物也造成伤害

KingSlime死亡时会发动最后一击



## 设置敌人注意事项

- 每个新敌人的Prefabs请检查挂载了所有的SO;
- 同种类敌人请检查GUID是否相同，若相同则设为DoNotPersisit再设置ReadWrite即可获取新GUID;
- Prefabs中有Data Defination组件，在场景中添加Prefabs时请自行手动更改GUID
- 添加BoxCollidor2D时请设置排除Layer为Player,Enemy的交互
- 请添加CapsuleCollidor2D用于PhysicsCheck