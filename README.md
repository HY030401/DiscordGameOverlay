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

### 4. 获取频道 ID

在 Discord 客户端中打开：

**用户设置 → 高级 → 开发者模式**

然后右键需要作为弹幕输入的文字频道，选择：

**复制频道 ID**

---

### 5. 创建本地配置文件

复制：

`config.example.json`

并将副本重命名为：

`config.json`

填写 Bot Token 和频道 ID：

```json
{
  "DiscordBotToken": "YOUR_BOT_TOKEN",
  "DiscordChannelId": 123456789012345678
}
