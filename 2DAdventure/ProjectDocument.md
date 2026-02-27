# 2DAdventure 项目说明（ProjectDocument）

> 本文档根据 `Assets/Scripts` 下代码与注释整理，架构图（事件流/存档结构/看板）进行归纳。
>
> 代码总体采用 **ScriptableObject 事件通道（Event Channel）** + **Additive Addressables 场景加载** + **ISaveable 存档接口** 的架构。
>
> 注：本项目在音乐音效方面有部分没有授权使用素材，严禁商用用途或者传播！

---

## 1. 游戏概述

- **类型**
  - 2D 横版动作/冒险。
- **核心循环（从代码推断）**
  - 从 **Menu 场景**进入游戏（Location）。
  - 在 Location 中移动、跳跃、攻击、滑铲；与场景交互物（宝箱、传送点、存档点）互动。
  - 与敌人（Bee/Boar/Snail/SlimeKing）遭遇并战斗；受伤/死亡触发 UI/动画。
  - 通过存档点保存进度，通过加载恢复进度与场景。

---

## 2. 玩家操作与玩法说明

### 2.1 移动与动作（`PlayerController`）

`PlayerController` 使用 `PlayerInputControl`（Unity Input System 生成的输入类）读取输入，并驱动 Rigidbody2D 运动与角色状态。

- **移动**
  - 每帧读取 `inputControl.Gameplay.Move`（Vector2）写入 `inputDirection`。
  - `FixedUpdate` 中当不处于受伤/攻击时执行 `Move()`。
  - `Move()` 通过设置 `rb.velocity` 实现水平移动。
  - 角色朝向通过 `transform.localScale.x` 翻转。

- **走/跑切换（强制走路机制）**
  - `WalkButton` 按下时将速度改为 `walkSpeed = speed / 2.5f`。
  - 松开时恢复 `runSpeed`。

- **跳跃 / 蹬墙跳**
  - `Gameplay.Jump.started` -> `Jump()`。
  - 在地面 `physicsCheck.isGround`：向上 `rb.AddForce(..., Impulse)`。
  - 在墙上 `physicsCheck.onWall`：施加反向+向上冲量，并设置 `wallJump = true`。

- **攻击**
  - `Gameplay.Attack.started` -> `PlayerAttack()`，触发 `PlayerAnimation.PlayAttack()` 并设置 `isAttack = true`。
  - `AttackFinish`（StateMachineBehaviour）在攻击动画进入/退出时写 `PlayerController.isAttack`，用于限制移动。

- **滑铲**
  - `Gameplay.Slide.started` -> `PlayerSlide()`。
  - 条件：未在滑铲中、在地面、体力足够。
  - 启动协程 `TriggerSlide`：
    - 将 `character.invulnerable = true`（滑铲无敌）
    - 通过 `rb.MovePosition(...)` 推进
    - 遇到悬崖/墙/方向变化则中断

### 2.2 交互系统（`Sign` + `IInterecatable`）

交互采用 **接口 + 触发器检测 + Confirm 按键**：

- 交互接口：`IInterecatable.TriggerAction()`（注意接口名拼写为 `IInterecatable`）。
- 玩家交互脚本 `Sign`：
  - `OnTriggerStay2D` 检测 `Tag == "Interecatable"` 的物体。
  - 缓存 `targetItem = collision.GetComponent<IInterecatable>()`。
  - 按下 `Confirm` 时调用 `targetItem.TriggerAction()`。
  - 同时在 `Sign` 自身 `GetComponent<AudioDefination>()?.PlayAudioClip()` 播放交互音效（音效脚本在 Sign 同一物体上时才会生效）。

### 2.3 生命/体力/受伤无敌（`Character`）

- `Character` 既用于 Player，也用于部分敌人（如 Snail 的技能状态会读取/写入 `Character.invulnerable`）。
- 关键变量：
  - `maxHealth/currentHealth`
  - `maxPower/currentPower/powerRecoverSpeed`
  - 受伤无敌：`invulnerableDuration/invulnerableCounter/invulnerable`

- 伤害处理：`TakeDamage(Attack attacker)`
  - 若 `invulnerable` 为 true 直接返回。
  - 扣血并触发 `TriggleInvulnerable()`。
  - 触发 UnityEvent：
    - `OnTakeDamage?.Invoke(attacker.transform)`
    - 死亡时 `OnDie?.Invoke()`
  - 最后 `OnHealthChange?.Invoke(this)` 更新 UI。

- 回血事件：`healthRecoverEvent`（`FloatEventSO`）
  - 在 `OnEnable()` 订阅：`healthRecoverEvent.OnEventEaised += OnhealthRecoverEvent`
  - `OnhealthRecoverEvent(float)` 仅当 `Layer == "Player"` 时生效，并将血量 clamp 到 `maxHealth`。

---

## 3. 场景系统与加载流程（Addressables Additive）

### 3.1 场景资源抽象（`GameSceneSO`）

`GameSceneSO`（ScriptableObject）保存场景元数据：

- `SceneType sceneType`：`Location` / `Menu`
- `AssetReference sceneReference`：Addressables 场景引用

### 3.2 启动流程（`InitialLoad` -> Persistent Scene）

- `InitialLoad` 在 `Awake()`：`Addressables.LoadSceneAsync(persistentScene)`
- persistent 场景中通常包含：
  - `SceneLoader`（场景切换核心）
  - `DataManager`（存档系统）
  - `AudioManager`、`FadeCanvas`、`UIManager`、`CameraControl` 等常驻系统

### 3.3 场景加载核心（`SceneLoader`）

`SceneLoader` 通过事件 `SceneLoadEventSO` 接收加载请求，并执行 **卸载旧场景 + Additive 加载新场景 + Fade**。

- **事件监听**
  - `loadEventSO.LoadRequestEvent += OnLoadRequestEvent`
  - `newGameEvent.OnEventEaised += NewGame`
  - `returnToMenuEvent.OnEventEaised += OnReturnToMenuEvent`

- **场景切换流程**
  - `OnLoadRequestEvent(sceneToGo, posToGo, fade)`：
    - 若已有 `currentLoadedScene`：启动协程 `UnloadPreviousScene()`
    - 否则 `LoadNewScene()`
  - `UnloadPreviousScene()`：
    - `fadeEvent.FadeIn(fadeDuration)`（黑屏）
    - `unLoadEventSO.RaiseEvent()` 通知 UI 等系统卸载
    - `yield return currentLoadedScene.sceneReference.UnLoadScene()`
    - `playerTrans.gameObject.SetActive(false)`
    - `LoadNewScene()`
  - `LoadNewScene()`：
    - `sceneToGo.sceneReference.LoadSceneAsync(Additive)`
    - `Completed += OnLoadCompleted`
  - `OnLoadCompleted()`：
    - `currentLoadedScene = sceneToGo`
    - `playerTrans.position = positionToGo`
    - `playerTrans.gameObject.SetActive(true)`
    - `fadeEvent.FadeOut(fadeDuration)`（透明）
    - `afterSceneLoadedEvent.RaiseEvent()`（仅 Location）

- **Menu/Location 的玩家物理控制**
  - `SceneLoader` 缓存 `playerRb` 并在加载请求与完成时根据 `sceneType` 设置 `playerRb.simulated`：
    - Menu：`simulated = false`（不下落）
    - Location：`simulated = true`

---

## 4. UI 系统

### 4.1 UI 入口（`UIManager`）

`UIManager` 监听多个事件，控制 HUD、GameOver、暂停菜单、音量滑块等。

- **平台判断（移动端 UI）**
  - `#if UNITY_STANDALONE` 关闭 `mobileTouch`。

- **事件监听**
  - `CharacterEventSO healthEvent`：更新血条/体力条
  - `SceneLoadEventSO sceneLoadEvent`：根据场景类型开关 HUD
  - `VoidEventSO loadDataEvent`：加载时关闭 GameOver 面板
  - `VoidEventSO GameOverEvent`：显示 GameOver 面板并选中 Restart
  - `VoidEventSO returnToMenuEvent`：复用 loadData 行为
  - `FloatEventSO syncVolumeEvent`：把 Mixer 的 dB 同步到 Slider

- **暂停**
  - `TogglePausePanel()`：
    - 打开暂停面板时 `pauseEvent.RaiseEvent()` 并 `Time.timeScale = 0`
    - 关闭时 `Time.timeScale = 1`

### 4.2 血条/体力条（`PlayerStatBar`）

- `OnhealthChange(float percentage)`：更新 `healthImage.fillAmount`
- `OnPowerChange(Character)`：进入体力恢复显示状态，`Update()` 中不断刷新 `powerImage.fillAmount`

### 4.3 Fade（`FadeCanvas` + `FadeEventSO`）

- `FadeCanvas` 订阅 `FadeEventSO.OnEventRaised`。
- 用 DOTween：`fadeImage.DOBlendableColor(target, duration)`。

---

## 5. 音频系统（事件驱动）

### 5.1 音频事件定义（`PlayAudioEventSO`）

- `PlayAudioEventSO`：`UnityAction<AudioClip> OnEventRaised`
- `RaiseEvent(AudioClip clip)`：广播播放请求

### 5.2 音频播放管理（`AudioManager`）

- 监听：
  - `FXEvent` / `BGMEvent`：播放音效/背景音乐
  - `FloatEventSO volumeChangeEvent`：滑块调音量 -> `AudioMixer.SetFloat("MasterVolume", ...)`
  - `VoidEventSO pauseEvent`：暂停时读取当前 Mixer 音量并广播 `syncVolumeEvent`

### 5.3 音效触发（`AudioDefination`）

- 将一个 `AudioClip` 与一个 `PlayAudioEventSO` 绑定。
- `PlayAudioClip()`：`playAudioEventSO.RaiseEvent(audioClip)`。
- 可选 `playOnEnable`：启用时自动播放。

### 5.4 音量映射（`VolumeMapper`）

- `SliderToDb(0~1) -> (-80dB~20dB)`
- `DbToSlider(dB) -> (0~1)`

---

## 6. 敌人系统（Enemy + 状态机）

### 6.1 Enemy 基类（`Enemy`）

- 必备组件：`Rigidbody2D`、`Animator`、`PhysicsCheck`。
- 持有状态：`patrolState/chaseState/skillState/currentState`。
- 生命周期：
  - `OnEnable()`：`currentState = patrolState; currentState.OnEnter(this)`
  - `Update()`：`currentState.LogicUpdate()` + 计时器
  - `FixedUpdate()`：移动 + `currentState.PhysicsUpdate()`
  - `OnDisable()`：`currentState.OnExit()`

- 玩家检测：`FindPlayer()`
  - 默认：`Physics2D.BoxCast`
  - Bee：覆写为 `OverlapCircle` 并记录 `attacker`

- 受伤/死亡事件方法：
  - `OnTakeDamage(Transform attackTrans)`：设置朝向、触发 `hurt` 动画、击退协程
  - `OnDie()`：改层为 2、播放死亡动画、标记 isDead

### 6.2 状态接口（`BaseState`）

- `OnEnter(Enemy)`
- `LogicUpdate()`
- `PhysicsUpdate()`
- `OnExit()`

### 6.3 Bee

- `BeePatrolState`
  - 在巡逻点间随机移动（`Bee.GetNowPoint()` 随机半径）
  - 发现玩家 -> `SwitchState(Chase)`
- `BeeChaseState`
  - 追踪玩家位置（考虑 Player 脚底偏移）
  - 距离满足攻击范围时按 `attackRate` 定时触发攻击动画

### 6.4 Boar

- `BoarPatrolState`
  - 行走动画开关；遇到悬崖/墙掉头（通过 `wait` 与动画 bool 控制）
- `BoarChaseState`
  - 追击动画 `run`；丢失目标超时切回 Patrol；遇到悬崖/墙会翻转方向

### 6.5 Snail

- `SnailPatrolState`
  - 发现玩家进入 `Skill`
- `SnailSkillState`
  - 进入时 `hide=true`、触发 `skill` 动画
  - 同时给 `Character.invulnerable = true`，并用 `lostTimeCounter` 作为无敌持续

### 6.6 SlimeKing（史莱姆王 / Boss）

SlimeKing 采用 `Enemy + BaseState` 结构，包含三态：

- `SlimeKingPatrolState`
  - 巡逻逻辑对齐 `SnailPatrolState`：未发现玩家时移动；遇到悬崖/墙体进入 `wait`，由 `Enemy.TimeCounter()` 负责等待结束后掉头。
  - 动画参数使用 `walk`（bool），巡逻移动时为 true，等待/停下时为 false。

- `SlimeKingChaseState`
  - 追击逻辑对齐 Bee 的“发现玩家后追击”的思路：持续刷新 `attacker`、并在丢失目标后倒计时结束切回 Patrol。
  - 动画参数使用 `chase`（bool），追击时为 true。

- `SlimeKingSkillState`（砸地攻击）
  - 攻击流程：
    - 进入 Skill：停止水平速度，触发 `attack`（trigger）。
    - 起跳：地面时对 `rb` 施加向上冲量（`jumpForce`）。
    - 落地：检测“曾离地 + 再次接地 + 下落阶段”，落地瞬间启用子物体 `attackHitbox` 一小段时间（`damageWindow`）。
    - 落地后进入僵直：使用 `Enemy.wait` + `attackStunTime`。
    - 冷却：额外使用 `attackCooldownCounter` 控制下一次 Skill 的最短间隔。

SlimeKing 特点：

- 本体不挂 `Attack`，不会“碰撞即伤害”，仅在 `attackHitbox` 开启的短窗口造成伤害。
- 可调参数暴露在 Inspector：`approachDistance/jumpForce/damageWindow/attackStunTime/attackCooldown`。

---

## 7. 战斗与伤害判定

### 7.1 攻击判定（`Attack`）

- `Attack` 通过 `OnTriggerStay2D` 对碰到的 `Character` 每个物理帧调用 `TakeDamage(this)`。
- 这意味着：
  - 若攻击碰撞体持续停留在目标身上，会产生“多次伤害”。
  - 实际伤害频率由 `FixedUpdate` 频率与 `Character.invulnerable` 控制。

### 7.2 受伤无敌（`Character.invulnerable`）

- 受击后 `TriggleInvulnerable()` 设置 `invulnerable=true` 并启动倒计时。
- `Update()` 中递减 `invulnerableCounter` 并在归零后取消无敌。

---

## 8. 存档/读档系统（ISaveable + DataManager + Data）

### 8.1 唯一 ID（`DataDefination`）

- 每个需要存档的物体挂 `DataDefination`。
- `persistentType == ReadWrite` 时在 `OnValidate()` 为 `ID` 生成 GUID。
- 存档 key 使用 `ID`（字符串）作为字典索引。

> 注意：目前代码仅在 `ID == null` 时生成 GUID；若 ID 为空字符串，可能导致未生成。

### 8.2 存档数据结构（`Data`）

- `sceneToSave : string`
- `characterPosDict : Dictionary<string, SerializeVector3>`
- `floatValueDict : Dictionary<string, float>`（例如 health/power）
- `boolValueDict : Dictionary<string, bool>`（预留给宝箱/开关等）

### 8.3 存档接口（`ISaveable`）

- `GetDataID()`：返回 `DataDefination`
- `GetSaveData(Data data)`：把自身状态写入 `data`
- `LoadData(Data data)`：从 `data` 恢复自身状态
- 默认实现：
  - `RegisterSaveData()` -> `DataManager.instance.RegisterSaveData(this)`
  - `UnRegisterSaveData()` -> `DataManager.instance.UnRegisterSaveData(this)`

### 8.4 存档管理（`DataManager`）

- 单例：`DataManager.instance`
- `saveableList` 保存所有注册的 `ISaveable`。
- `Save()`：遍历 `saveableList` 调用 `GetSaveData(saveData)`，用 Newtonsoft Json 序列化写到：
  - `Application.persistentDataPath + "/SAVE DATA/data.sav"`
- `Load()`：先从磁盘读回 `saveData`，再遍历调用 `LoadData(saveData)`。
- 开发者模式：`inDeveloperModule` 时按 `L` 可触发 Load。

### 8.5 场景的存取（`SceneLoader` 与 `Data.SaveGameScene`）

- `SceneLoader.GetSaveData`：`data.SaveGameScene(currentLoadedScene)`
- `Data.SaveGameScene`：`sceneToSave = JsonUtility.ToJson(savescene)`
- `Data.GetSavedScene`：运行时创建 `GameSceneSO` 并 `FromJsonOverwrite`。

> **重要说明（根据 Unity 序列化规则）**
>
> 这里保存的不是“运行中 Scene 的全部对象状态”，而是 `GameSceneSO` 的可序列化字段快照。
> 并且 Addressables 的 `AssetReference` 在 JSON 序列化/反序列化时存在兼容性风险：即使能写入字符串，也不等于能稳定恢复为可用引用。
>
> 因此项目中真正的“进度保存”仍依赖 `Data` 的几个字典（位置、血量、布尔状态等），而不是场景对象本身。

### 8.6 存档点（`SavePointRock`）

- `SavePointRock : IInterecatable`
- 第一次触发交互后：
  - 切换精灵/开灯
  - `SaveGameEvent.RaiseEvent()` -> 通常由 `DataManager` 监听并执行 `Save()`
  - 关闭 collider，防止重复存档

---

## 9. 关卡交互物

### 9.1 传送点（`TelePoint`）

- `TelePoint : IInterecatable`
- 触发后：
  - 关闭 collider
  - `loadEventSO.RasieLoadRequestEvent(sceneToGo, positionToGo, true)` 请求场景加载

### 9.2 宝箱（`Chest`）

- `Chest : IInterecatable, ISaveable`
- 当前实现：
  - 未打开时触发 `OpenChest()`
  - 广播 `healthRecoverEvent.RaiseEvent(healthRecoverValue)`
  - 禁用 collider 防止重复触发
- 存档部分目前处于“待开发/注释状态”，`GetSaveData/LoadData` 逻辑被注释。

---

## 10. 事件系统（EventSO）总览

项目核心解耦方式是：**用 ScriptableObject 存放事件（Event Channel）**。这样 UI、场景、音频、存档都不需要互相引用对方脚本实例。

### 10.1 EventSO 类型

- **`VoidEventSO`**
  - `UnityAction OnEventEaised`
  - `RaiseEvent()`
  - 用于：保存/读档、暂停、场景卸载、场景加载完成、返回菜单等。

- **`FloatEventSO`**
  - `UnityAction<float> OnEventEaised`
  - `RaiseEvent(float amount)`
  - 用于：音量变化、音量同步、宝箱回血等。

- **`SceneLoadEventSO`**
  - `UnityAction<GameSceneSO, Vector3, bool> LoadRequestEvent`
  - `RasieLoadRequestEvent(GameSceneSO, Vector3, bool)`
  - 用于：发起场景加载请求（TelePoint/UI/SceneLoader 自身等都可 Raise）。

- **`FadeEventSO`**
  - `UnityAction<Color, float> OnEventRaised`
  - `FadeIn/FadeOut/RaiseEvent`
  - 用于：黑屏/透明渐变。

- **`PlayAudioEventSO`**
  - `UnityAction<AudioClip> OnEventRaised`
  - `RaiseEvent(AudioClip)`
  - 用于：请求播放 BGM/FX。

- **`CharacterEventSO`**
  - `UnityAction<Character> OnEventRaised`
  - `RasieEvent(Character)`
  - 用于：把角色状态（血/体力）广播给 UI。

### 10.2 关键事件流（对照你给的架构图）

- **场景加载流程**
  - `TelePoint` / `UIManager` / `SceneLoader` -> `SceneLoadEventSO.RasieLoadRequestEvent(...)`
  - `SceneLoader` 监听 `SceneLoadEventSO.LoadRequestEvent` -> `OnLoadRequestEvent`
  - `SceneLoader` -> `FadeEventSO` -> `FadeCanvas` 执行渐变
  - `SceneLoader` -> `afterSceneLoadedEvent` -> `PlayerController` / `CameraControl` / `UIManager` 等做“加载后初始化”

- **存档/读档流程**
  - `SavePointRock` -> `SaveGameEvent (VoidEventSO)` -> `DataManager.Save()`
  - `loadDataEvent (VoidEventSO)` -> `DataManager.Load()` -> 遍历各 `ISaveable.LoadData`

- **音频与音量**
  - UI Slider -> `volumeChangeEvent(FloatEventSO)` -> `AudioManager` 写入 Mixer
  - 暂停 -> `pauseEvent(VoidEventSO)` -> `AudioManager` 读取 Mixer 并 `syncVolumeEvent(FloatEventSO)` -> `UIManager` 同步 Slider

---

## 10.3 各功能“实现流”（更细的逐步执行顺序）

本节把常见功能按“从触发到结果”的顺序展开，方便你对照断点/日志定位问题。

### 10.3.1 场景切换（TelePoint/菜单 -> SceneLoader -> Fade -> AfterLoaded）

#### A. 触发入口（谁会发起加载请求）

- `TelePoint.TriggerAction()`
  - 关闭自身 `BoxCollider2D`，防止重复触发。
  - 调用 `loadEventSO.RasieLoadRequestEvent(sceneToGo, positionToGo, true)`。
- `SceneLoader.Start()`
  - 游戏开始时直接发起一次加载请求：`loadEventSO.RasieLoadRequestEvent(menuScene, menuPosition, true)`。
- `SceneLoader.NewGame()`
  - 由 `newGameEvent` 触发：设置 `sceneToGo = firstLoadScene`、`playerTrans.position = firstPosition`。
  - 再 `RasieLoadRequestEvent(firstLoadScene, firstPosition, true)`。

#### B. SceneLoader 收到加载请求后的顺序

1. `SceneLoader.OnLoadRequestEvent(locationToGo, posToGo, fadeScreen)`
   - **防重入**：若 `isLoading == true` 直接 return。
   - 缓存本次目标：`sceneToGo/locationToGo`、`positionToGo/posToGo`、`fadeScreen`。
   - 若 `currentLoadedScene != null`
     - 启动协程 `UnloadPreviousScene()` 走“先卸载再加载”。
   - 否则
     - 直接 `LoadNewScene()`。

2. 同一函数内处理“Menu 场景禁用玩家物理”
   - 缓存 `playerRb = playerTrans.GetComponent<Rigidbody2D>()`。
   - 若 `sceneToGo.sceneType == SceneType.Location`：`playerRb.simulated = true`。
   - 否则（Menu）：`playerRb.simulated = false`。

3. 若需要卸载旧场景：`SceneLoader.UnloadPreviousScene()`
   - 若 `fadeScreen == true`：调用 `fadeEvent.FadeIn(fadeDuration)`（黑屏）。
   - 广播 `unLoadEventSO.RaiseEvent()`（用来通知 UI/其它系统：旧场景即将卸载）。
   - `yield return new WaitForSeconds(fadeDuration)`（等待黑屏完成）。
   - `yield return currentLoadedScene.sceneReference.UnLoadScene()`（Addressables 卸载旧 scene）。
   - `playerTrans.gameObject.SetActive(false)`。
   - 调用 `LoadNewScene()`。

4. 加载新场景：`SceneLoader.LoadNewScene()`
   - `sceneToGo.sceneReference.LoadSceneAsync(LoadSceneMode.Additive)`
   - `Completed += OnLoadCompleted`

5. 加载完成回调：`SceneLoader.OnLoadCompleted(handle)`
   - 写入当前场景：`currentLoadedScene = sceneToGo`。
   - 定位玩家：`playerTrans.position = positionToGo`。
   - 激活玩家：`playerTrans.gameObject.SetActive(true)`。
   - 再次根据 `currentLoadedScene.sceneType` 设置 `playerRb.simulated`。
   - 若 `fadeScreen == true`：调用 `fadeEvent.FadeOut(fadeDuration)`（从黑屏恢复）。
   - `isLoading = false`。
   - **仅当 Location**：`afterSceneLoadedEvent.RaiseEvent()`（Menu 不广播）。

#### C. afterSceneLoadedEvent 会影响哪些系统（已在代码出现的监听者）

- `PlayerController.OnAfterSceneLoadedEvent()`
  - `inputControl.Gameplay.Enable()`（允许移动/跳跃/攻击输入）。
- `CameraControl.OnAfterSceneLoadEvent()`
  - `GetNewCameraBounds()`，查找 Tag 为 `Bounds` 的 collider 作为 CinemachineConfiner2D 的新边界。

### 10.3.2 存档（SavePointRock -> SaveGameEvent -> DataManager.Save -> 写文件）

1. 玩家交互 `Sign` 检测到 `Tag == "Interecatable"`，按下 Confirm。
2. `Sign.OnConfirm()` 调用 `targetItem.TriggerAction()`。
3. `SavePointRock.TriggerAction()`
   - 若 `isDone == false` 执行 `saveGame()`。
4. `SavePointRock.saveGame()`
   - `isDone = true`，切换 sprite，开启 light。
   - `SaveGameEvent.RaiseEvent()`。
   - 禁用自己的 `BoxCollider2D`。
5. `DataManager` 监听 `saveDataEvent.OnEventEaised += Save`（`VoidEventSO`）
6. `DataManager.Save()`
   - 遍历 `saveableList`：对每个对象调用 `saveable.GetSaveData(saveData)`。
   - `JsonConvert.SerializeObject(saveData)`。
   - 写入 `Application.persistentDataPath + "/SAVE DATA/data.sav"`。

### 10.3.3 读档（loadDataEvent -> DataManager.Load -> 各 ISaveable.LoadData）

1. 某处触发 `loadDataEvent.RaiseEvent()`（例如 UI 的“Load”按钮、开发者模式按键、或你的其它逻辑）。
2. `DataManager.Load()`
   - 若存档文件不存在：显示 `NoSave` UI 0.5 秒后关闭。
   - 否则调用 `ReadSavedData()` 先把文件反序列化回 `saveData`。
   - 遍历 `saveableList` 调用 `saveable.LoadData(saveData)`。

3. 常见 LoadData 行为（当前项目代码里的实际表现）
   - `Character.LoadData(data)`
     - 读取位置：`transform.position = data.characterPosDict[ID]`。
     - 读取数值：`currentHealth/currentPower`。
     - 立刻 `OnHealthChange?.Invoke(this)`（用于 UI 更新）。
   - `SceneLoader.LoadData(data)`
     - 若 `data.characterPosDict` 中存在 player 的 ID：
       - `positionToGo = 保存的位置`
       - `sceneToGo = data.GetSavedScene()`
       - 调用 `OnLoadRequestEvent(sceneToGo, positionToGo, true)` 发起切场景。

> 注意：当前 `Data.SaveGameScene/GetSavedScene` 使用 `JsonUtility` 保存 `GameSceneSO` 快照。
> `AssetReference` 的 JSON 还原不一定稳定，因此切换场景的“长期可靠方案”通常是保存 Addressables key/address（字符串）。

### 10.3.4 交互（进入触发器 -> Sign 显示提示 -> Confirm -> TriggerAction）

1. 玩家进入交互物触发器范围
   - `Sign.OnTriggerStay2D(Collider2D collision)`
   - 若 `collision.CompareTag("Interecatable")`：
     - `canPress = true`
     - `targetItem = collision.GetComponent<IInterecatable>()`

2. UI 提示的显示
   - `Sign.Update()` 每帧把 `signSprite` renderer 的 enabled 设置为 `canPress`。
   - `signSprite.transform.localScale = PlayerTrans.localScale` 保证提示朝向跟随玩家。

3. 玩家按下确认键
   - `playerInput.Gameplay.Confirm.started += OnConfirm`
   - `Sign.OnConfirm()`：
     - 若 `canPress`：
       - 调用 `targetItem.TriggerAction()`
       - 尝试播放音效：`GetComponent<AudioDefination>()?.PlayAudioClip()`（AudioDefination 必须挂在 Sign 同物体上才会响）

### 10.3.5 宝箱回血（Chest -> healthRecoverEvent(Float) -> Character.OnhealthRecoverEvent）

1. `Chest.TriggerAction()`
   - 若 `isDone == false`：
     - `OpenChest()`：换 openSprite、`isDone = true`、禁用 BoxCollider2D。
     - `healthRecoverEvent.RaiseEvent(healthRecoverValue)`。

2. `Character.OnEnable()` 中订阅：`healthRecoverEvent.OnEventEaised += OnhealthRecoverEvent`。
3. `Character.OnhealthRecoverEvent(float value)`
   - 若当前对象不是 Player（通过 layer 名称判断）直接 return。
   - `currentHealth = min(currentHealth + value, maxHealth)`
   - `OnHealthChange?.Invoke(this)` 更新 UI。

### 10.3.6 伤害与受伤反馈（Attack -> Character.TakeDamage -> Enemy.OnTakeDamage/OnDie -> UI）

#### A. 伤害命中触发

1. 攻击体（`Attack`）与目标（`Character`）触发器重叠。
2. `Attack.OnTriggerStay2D(other)`
   - `other.GetComponent<Character>()?.TakeDamage(this)`

#### B. Character 扣血与无敌

3. `Character.TakeDamage(attacker)`
   - 若 `invulnerable == true`：直接 return（同一次重叠不会重复扣血）。
   - 若 `currentHealth - damage > 0`
     - 扣血
     - `TriggleInvulnerable()` 启动受伤无敌计时
     - `OnTakeDamage?.Invoke(attacker.transform)`（把攻击者 Transform 传出去）
   - 否则
     - `currentHealth = 0`
     - `OnDie?.Invoke()`
   - 最后 `OnHealthChange?.Invoke(this)`

4. `Character.Update()`
   - 若 `invulnerable == true`：递减 `invulnerableCounter`，归零后关闭无敌。

#### C. 敌人收到受伤事件

5. 敌人的 `Character.OnTakeDamage` 通常会在 Inspector 里绑定到 `Enemy.OnTakeDamage(Transform)`
6. `Enemy.OnTakeDamage(attackTrans)`
   - 根据攻击者位置翻转朝向
   - `isHurt = true`，`anim.SetTrigger("hurt")`
   - `rb.AddForce(dir * hurtForce, Impulse)` 产生击退
   - 协程结束后 `isHurt = false`

7. 死亡：`Enemy.OnDie()`
   - 改 layer=2（Ignore Raycast）
   - `anim.SetBool("dead", true)`
   - `isDead = true`

### 10.3.7 UI 刷新（CharacterEventSO/OnHealthChange -> UIManager -> PlayerStatBar）

当前 UI 刷新有两条常见来源：

- **来源 1：`Character.OnHealthChange`（UnityEvent）直接绑定**
  - 由 Inspector 将 `Character.OnHealthChange` 绑定到某个脚本方法（例如 UI 事件通道/或 PlayerStatBar）。
- **来源 2：`CharacterEventSO` 事件通道**
  - `UIManager` 在 `OnEnable()`：`healthEvent.OnEventRaised += OnHealthEvent`。
  - `UIManager.OnHealthEvent(character)`：
    - 算 `percentage = currentHealth/maxHealth`
    - `playerStatBar.OnhealthChange(percentage)`
    - `playerStatBar.OnPowerChange(character)`（开始播放体力恢复条）

### 10.3.8 音量滑块与混音器（UI -> FloatEventSO -> AudioManager -> Mixer）

1. UI Slider 值变化（0~1）
2. 通过 `volumeChangeEvent.RaiseEvent(value)` 广播给 `AudioManager`。
3. `AudioManager.OnVoluneChangeEvent(amount)`
   - `Mixer.SetFloat("MasterVolume", VolumeMapper.SliderToDb(amount))`

### 10.3.9 暂停时同步音量（Pause -> AudioManager 读取 Mixer -> UIManager 设置 Slider）

1. 点击 Settings
   - `UIManager.TogglePausePanel()`
   - 打开暂停面板前先 `pauseEvent.RaiseEvent()`

2. `AudioManager.OnpauseEvent()`
   - `Mixer.GetFloat("MasterVolume", out amount)`
   - `syncVolumeEvent.RaiseEvent(amount)`

3. `UIManager.OnSyncVolumeEvent(amount)`
   - `VolumeSlider.value = VolumeMapper.DbToSlider(amount)`

---

## 11. 代码目录结构与脚本分类说明

> 本节按 `Assets/Scripts` 文件夹分类说明每份代码用途、关键函数与依赖关系。

### 11.1 `Transition/`（场景切换）

- **`InitialLoad.cs`**
  - **职责**：从初始启动场景加载 persistent 场景。
  - **关键函数**：`Awake()` -> `Addressables.LoadSceneAsync(persistentScene)`。

- **`SceneLoader.cs`**
  - **职责**：全局场景加载/卸载控制器（Additive + Fade + 广播）。
  - **关键函数**：
    - `OnLoadRequestEvent(GameSceneSO, Vector3, bool)`
    - `UnloadPreviousScene()`
    - `LoadNewScene()`
    - `OnLoadCompleted(...)`
    - `GetSaveData(Data)` / `LoadData(Data)`（与存档系统集成）
  - **依赖**：
    - 事件：`SceneLoadEventSO`、`FadeEventSO`、`afterSceneLoadedEvent`、`unLoadEventSO`
    - 数据：`GameSceneSO`、`DataDefination`

- **`TelePoint.cs`**
  - **职责**：传送点交互，发起场景加载请求。
  - **关键函数**：`TriggerAction()` -> `loadEventSO.RasieLoadRequestEvent(...)`。

### 11.2 `SaveLoad/`（存档系统）

- **`ISaveable.cs`**
  - **职责**：存档对象统一接口；提供默认注册/反注册到 `DataManager` 的实现。

- **`DataManager.cs`**
  - **职责**：存档管理器（收集 ISaveable、序列化 Data、写文件/读文件）。
  - **关键函数**：`Save()`、`Load()`、`ReadSavedData()`、`RegisterSaveData()`。

- **`Data.cs`**
  - **职责**：存档数据模型（场景信息 + 多类字典）。
  - **关键函数**：`SaveGameScene()`、`GetSavedScene()`。

- **`DataDefination.cs`**
  - **职责**：为存档对象提供 GUID 字符串 ID。
  - **关键函数**：`OnValidate()`。

- **`SavePointRock.cs`**
  - **职责**：存档点交互物，触发保存事件。
  - **关键函数**：`TriggerAction()`、`saveGame()`。

### 11.3 `ScriptableObject/`（事件通道与场景 SO）

- `VoidEventSO.cs`
- `FloatEventSO.cs`
- `SceneLoadEventSO.cs`
- `FadeEventSO.cs`
- `PlayAudioEventSO.cs`
- `CharacterEventSO.cs`
- `GameSceneSO.cs`

> 这些文件共同组成项目的“事件总线”与“场景引用层”。

### 11.4 `Player/`（玩家）

- **`PlayerController.cs`**
  - **职责**：输入、移动、跳跃、滑铲、攻击状态控制。
  - **关键函数**：`Move()`、`Jump()`、`PlayerAttack()`、`PlayerSlide()`、`TriggerSlide()`。
  - **事件**：监听 `SceneLoadEventSO`、`afterSceneLoadedEvent`、`loadDataEvent`。

- **`PlayerAnimation.cs`**
  - **职责**：根据 Rigidbody2D/PhysicsCheck/PlayerController 状态设置 Animator 参数。
  - **关键函数**：`SetAnimation()`、`PlayHurt()`、`PlayAttack()`。

- **`AttackFinish.cs`**
  - **职责**：攻击动画状态机回调，控制 `isAttack`。

- **`HurtAnimation.cs`**
  - **职责**：受伤动画结束时复位 `isHurt`。

- **`Sign.cs`**
  - **职责**：玩家交互检测与确认键触发。
  - **关键函数**：`OnTriggerStay2D`、`OnConfirm()`。

### 11.5 `General/`（通用角色/交互/战斗）

- **`Character.cs`**
  - **职责**：血量/体力/无敌/受伤与死亡事件、存档集成。
  - **关键函数**：`TakeDamage()`、`TriggleInvulnerable()`、`LoadData()`、`GetSaveData()`、`OnhealthRecoverEvent()`。

- **`Attack.cs`**
  - **职责**：攻击判定组件（触发器 stay 造成持续伤害）。
  - **关键函数**：`OnTriggerStay2D()`。

- **`PhysicsCheck.cs`**
  - **职责**：地面/墙体检测；对 Player 额外计算 `onWall`。
  - **关键函数**：`Check()`。

- **`Chest.cs`**
  - **职责**：宝箱交互 + 回血事件广播（存档逻辑待补全）。
  - **关键函数**：`TriggerAction()`、`OpenChest()`。

### 11.6 `Enemy/`（敌人 AI）

- **`Enemy.cs`**：敌人基类 + 状态机驱动。
- **`BaseState.cs`**：状态抽象。
- **`Bee.cs` / `BeePatrolState.cs` / `BeeChaseState.cs`**
- **`Boar.cs` / `BoarPatrolState.cs` / `BoarChaseState.cs`**
- **`Snail.cs` / `SnailPatrolState.cs` / `SnailSkillState.cs`**

### 11.7 `UI/`（UI）

- **`UIManager.cs`**：HUD、暂停、GameOver、音量同步。
- **`PlayerStatBar.cs`**：血条/体力条。
- **`FadeCanvas.cs`**：画面淡入淡出。
- **`Menu.cs`**：主菜单按钮选中与退出。
- **`LightControl.cs`**：Boss 死亡后全局光（`Light2D`）变亮（DOTween）。
- **`BossBackgroundSwap.cs`**：Boss 死亡后切换背景 `SpriteRenderer.sprite`（可选淡入淡出）。

### 11.8 `Audio/`（音频）

- **`AudioManager.cs`**：事件驱动音频中心。
- **`AudioDefination.cs`**：单个物体绑定音频与事件通道。

### 11.9 `Utilities/`（工具与枚举）

- **`Enums.cs`**
  - `NPCState`：Patrol/Chase/Skill
  - `SceneType`：Location/Menu
  - `PersistentType`：ReadWrite/DoNotPersist

- **`IInterecatable.cs`**：交互接口
- **`CameraControl.cs`**：监听 afterSceneLoadEvent 更新 Cinemachine Confiner 边界；监听 caremaShakeEvent 触发 Impulse
- **`VolumeMapper.cs`**：音量映射

---

## 12. 常见扩展点 / 需要注意的实现细节

- **宝箱存档未完成**：`Chest` 的 `GetSaveData/LoadData` 目前注释掉，若要保存宝箱开关状态需要恢复这部分逻辑并确保 `DataDefination.ID` 稳定唯一。

- **`DataDefination` 的 ID 生成条件**：当前只判断 `ID == null`，若为空字符串可能无法生成 GUID，可能导致存档 key 冲突。

- **`Attack` 的伤害频率**：`OnTriggerStay2D` 会导致持续扣血；若想“每次挥刀只命中一次”，通常改为 `OnTriggerEnter2D` 或加入命中冷却/已命中集合。

- **Player Build 编译失败（UnityEditor 引用）**：运行时代码（非 Editor 文件夹/非条件编译）不要引用 `UnityEditor` 命名空间，否则打包时会报缺失类型/命名空间（例如 `UnityEditor.FilePathAttribute`）。

- **`GameSceneSO` 的序列化可靠性**：使用 `JsonUtility` 保存 `AssetReference` 有潜在风险。更稳的方式通常是保存 Addressables 的 key/address（字符串）或自定义可序列化结构。

- **时间缩放**：暂停使用 `Time.timeScale = 0`，相关协程若使用 `WaitForSeconds` 会停住；需要实时等待时应使用 `WaitForSecondsRealtime`（本项目 Fade 协程在 SceneLoader 使用 `WaitForSeconds(fadeDuration)`，暂停期间若触发切场景可能需额外注意）。

---

## 13. 文档更新建议

当你后续补充：
- 新关卡/新交互物
- 完善 Chest 的存档
- 引入更多 EventSO

建议同步更新：
- **第 10 章事件总览**（新增事件、触发者、监听者）
- **第 11 章脚本分类**（新增文件职责与依赖）

---

## 14. 附：根据架构图的事件关系简表（文字版）

- **SceneLoader**
  - Raise：`FadeEventSO`、`afterSceneLoadedEvent`、`unLoadEventSO`
  - Listen：`SceneLoadEventSO`、`newGameEvent`、`returnToMenuEvent`

- **UIManager**
  - Listen：`healthEvent(CharacterEventSO)`、`sceneLoadEvent(SceneLoadEventSO)`、`loadDataEvent(Void)`、`GameOverEvent(Void)`、`returnToMenuEvent(Void)`、`syncVolumeEvent(Float)`
  - Raise：`pauseEvent(Void)`（暂停）

- **AudioManager**
  - Listen：`volumeChangeEvent(Float)`、`FXEvent/BGMEvent(PlayAudioEventSO)`、`pauseEvent(Void)`
  - Raise：`syncVolumeEvent(Float)`

- **DataManager**
  - Listen：`saveDataEvent(Void)`、`loadDataEvent(Void)`
  - 对外提供：`RegisterSaveData/UnRegisterSaveData`

- **交互物**
  - `TelePoint` Raise：`SceneLoadEventSO`
  - `SavePointRock` Raise：`SaveGameEvent(VoidEventSO)`
  - `Chest` Raise：`healthRecoverEvent(FloatEventSO)`

