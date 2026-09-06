## Discord Bot 配置与添加到服务器

本项目需要一个 Discord Bot，用于读取指定文字频道中的消息。

### 1. 创建 Discord Application

打开 Discord Developer Portal：

https://discord.com/developers/applications

点击 **New Application** 创建一个新的应用。

创建完成后，进入该应用的管理页面。

---

### 2. 配置 Bot

在左侧菜单进入：

**Bot / 机器人**

确认应用已经拥有 Bot。

在 **Privileged Gateway Intents** 中开启：

- Message Content Intent

该权限用于读取 Discord 消息正文。

然后在 Bot 页面复制：

- Bot Token (令牌)

> 注意：Bot Token 相当于机器人的密码，请勿上传到 GitHub 或分享给他人。

---

### 3. 配置服务器安装权限

进入左侧：

**Installation / 安装**

在服务器安装的 Scope 中添加：

- `bot`
- `applications.commands`

Bot 权限至少需要：

- View Channels / 查看频道
- Read Message History / 阅读消息历史记录

如果以后需要让 Bot 主动发送消息，还可以添加：

- Send Messages / 发送消息

完成后复制 Discord 提供的安装链接，并在浏览器中打开。

选择需要添加机器人的 Discord 服务器并完成授权。

---

### 3. 将 Bot 添加到 Discord 服务器

进入 Developer Portal 左侧的：

**Installation / 安装**

在服务器安装的 Scope 中添加：

- `bot`
- `applications.commands`

Bot 至少需要以下权限：

- View Channels / 查看频道
- Read Message History / 阅读消息历史记录

如果以后需要 Bot 主动发送消息，还可以增加：

- Send Messages / 发送消息

完成后复制 Discord 提供的安装链接，在浏览器中打开，并选择需要添加机器人的服务器。

---

### 4. 获取弹幕频道 ID

在 Discord 客户端中打开：

**用户设置 → 高级 → 开发者模式**

然后右键需要读取弹幕的文字频道，选择：

**复制频道 ID**

---

### 5. 在程序中填写配置

启动 Discord Game Overlay。

在配置界面中填写：

- **Bot Token**
- **Discord Channel ID**

其中：

- Bot Token：从 Discord Developer Portal 的 Bot 页面获取
- Channel ID：从目标 Discord 文字频道复制得到

保存配置后，程序会使用该 Bot 连接 Discord，并监听指定文字频道。

---

### 6. 测试

在已配置的 Discord 文字频道中发送一条消息，例如：

`测试弹幕`

如果配置正确，该消息将被程序读取并显示为弹幕。

如果无法读取消息，请检查：

- Bot 是否已经成功加入服务器
- Message Content Intent 是否已经开启
- Bot 是否拥有目标频道的查看权限
- Channel ID 是否填写正确

---

## 自定义互动表情

程序只响应配置过的服务器自定义表情，不会响应 Discord 内置的
`💩`、`❤️` 或 `🥚`。

### 1. 上传表情

在 Discord 的服务器设置中进入 **表情 / Emoji**，上传：

- `src/DiscordGameOverlay/Assets/DiscordEmojis/throw_poop.png`
- `src/DiscordGameOverlay/Assets/DiscordEmojis/pigeon_poop.png`
- `src/DiscordGameOverlay/Assets/DiscordEmojis/heart_arrow.png`
- `src/DiscordGameOverlay/Assets/DiscordEmojis/throw_egg.png`

建议分别命名为：

- `throw_poop` — `1546008982026584094`
- `pigeon_poop` — `1546035873668276234`
- `heart_arrow` — `1546009038943424522`
- `throw_egg` — `1546009077107269794`

### 2. 获取表情 ID

在 Discord 消息框中先输入反斜杠，再选择自定义表情。例如：

`\:heart_arrow:`

发送后 Discord 会显示类似：

`<:heart_arrow:123456789012345678>`

最后一段数字就是需要填写的表情 ID。

### 3. 填写设置

打开程序的设置窗口，分别填写：

- 扔屎自定义表情 ID
- 鸽子屎自定义表情 ID
- 爱心中箭自定义表情 ID
- 扔鸡蛋自定义表情 ID

保存并重新启动程序。之后发送这些自定义表情，或把它们作为消息反应，
即可触发对应特效。

其中扔屎与扔鸡蛋共用迎面投掷轨迹；鸽子屎从窗口上方落下，
落到底部后摊开并在约 2 秒后淡出。

### 4. 聚合、强度与冷却

每一种特效独立使用同一套规则：

- 第一枚非冷却状态的表情会开启 3 秒计数窗口。
- 3 秒结束时，1～5 枚触发一级强度并播放 1 个实例。
- 6～10 枚触发二级强度并播放 3 个实例。
- 11 枚及以上触发三级强度并播放 5 个实例。
- 动画批次生成后进入 20 秒冷却；冷却期间的对应表情不触发动画。
- 被识别为动画触发的消息不会进入主播或观众弹幕画面。
