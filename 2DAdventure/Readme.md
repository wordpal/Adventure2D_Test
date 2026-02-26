## Todo

最终Boss

镜头看向Boss再转移回来



## 设置敌人注意事项

- 每个新敌人的Prefabs请检查挂载了所有的SO;
- 同种类敌人请检查GUID是否相同，若相同则设为DoNotPersisit再设置ReadWrite即可获取新GUID;
- Prefabs中有Data Defination组件，在场景中添加Prefabs时请自行手动更改GUID