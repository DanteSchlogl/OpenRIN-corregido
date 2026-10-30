# OpenRIN — 抗凝患者综合预警与监测平台

**作者：** Carlos Santiago Muñiz Cortés
**教师：** Gastón Matías Weingand
**院校：** Colegio Leonardo Da Vinci
**课程：** Prácticas Profesionalizantes III（职业实践 III） · **年份：** 2026

[Español](README.md) · [English](README.en.md) · [中文]

## 简介

OpenRIN 是一款面向血液科诊所的桌面平台，专注于抗凝患者的随访管理：统一管理患者与临床病历、接收 RIN（INR）检验值、自动进行危急程度分级并生成预警、带智能改期的预约排班、含联系与升级流程的临床随访、疗效统计报告、患者门户，以及系统管理与安全工具。

本方案基于 **C# / .NET 8（Windows Forms）** 与 **SQL Server** 构建，采用多层架构，完整覆盖诊所业务闭环：患者 → RIN 检查 → 预警 → 预约 → 随访 → 报告。

## 本版本新特性（v1.0）

- **主动完整性**：按行与按表的校验位（DVH/DVV），在唯一的保存点签名，并在**启动应用时、登录之前**进行校验。
- **组合模式（Composite）权限**：原子权限与组合权限构成的树，具有唯一编码，可在系统内编辑（勾选/取消分组），并应用于模块访问控制。
- **动态国际化**：西班牙语（拉美）、英语与简体中文，语言与词条**存储在数据库中**（运行时无静态资源文件）；通过观察者模式实现热切换。
- **带恢复功能的变更控制**：审计历史包含变更前后状态，并提供“恢复上一状态”操作（恢复并重新签名）。
- **使用第三方库生成 PDF**（QuestPDF，社区许可）用于报告与病历，同时支持 **Excel** 与 **JSON** 导出。
- **自动备份**：每日（03:00）备份两个数据库，并对每份副本进行完整性校验。
- **完整的项目文档册**（约 150 页）位于 [`Documentacion/`](Documentacion/)。

## 仓库结构

- `Codigo/` —— .NET 解决方案：`Negocio.UI`（Windows Forms）、`Negocio.BLL`、`Negocio.DAL`、`Negocio.DomainModel` 以及 `Services`（DomainModel、BLL、DAL、Facade），另有冒烟测试。
- `Documentacion/` —— **项目文档册（PDF）**：总体描述、类图与数据图、业务流程与用例、技术专题。
- `Views/` —— 分析设计交付物（历史存档）。
- `Propuesta Corregida/` —— 项目提案（TP 1）。
- `Diagramas/` —— 修正后的图表（draw.io 与 PNG）及其再生成源码。
- `ops/` —— 每日自动备份（03:00）脚本：SQL、计划任务与说明。

## 架构

- **分层**：表示层（WinForms）→ 业务逻辑层（BLL）与服务门面（Facade）→ 数据访问层（DAL）→ 数据库。
- **两条持久化路径**：业务域采用 **Entity Framework Core**（Fluent 配置与唯一保存点），服务库采用 **ADO.NET**（安全、语言、权限、日志）。
- **数据库**：`OpenRIN_Negocio`（13 张表，含完整性签名与变更审计）与 `OpenRIN_Services`（安全、语言、权限与日志）。
- **设计模式**：Singleton（会话）、Composite（权限）、Observer（语言）、Repository（患者）与 Facade（技术服务）。

## 运行要求

- Windows 10/11。
- .NET 8（Desktop）。
- SQL Server 2019 或更高（Express 版即可）。

> 凭据与本地配置**不纳入**本仓库版本管理。

## 运行方式

1. 创建 `OpenRIN_Negocio` 与 `OpenRIN_Services` 数据库，并执行结构与初始数据脚本（见 `Codigo/`）。
2. 配置本地连接字符串（配置文件不纳入版本管理）。
3. 编译并运行：`dotnet build`，然后运行 `Negocio.UI` 项目。

启动时，应用会在显示登录界面**之前**校验数据库完整性。

## 完整文档

**[项目文档册](Documentacion/)** 包含完整的交付文档：产品全局说明、各层类图、数据模型、五个业务过程与**十三个用例**（含顺序图、受影响类与真实系统截图），以及技术专题（架构、会话、加密、权限配置、多语言、活动日志、变更控制、备份与完整性）。

## 项目状态

2026 年 11 月交付。后续计划：安装程序、用户手册与在线帮助。
