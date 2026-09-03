# TTF Font Loader Plugin_Thai Font

ปลั๊กอิน BepInEx ตัวนี้ช่วยให้เกม Unity (ทั้งระบบ IL2CPP และ Mono) สามารถโหลดและใช้งานไฟล์ฟอนต์ .ttf ได้โดยตรง เพื่อนำมาใช้เปลี่ยนการแสดงผลฟอนต์ภายในเกม


## คุณสมบัติหลัก

- โหลดไฟล์ฟอนต์ `.ttf` ที่วางไว้ใน โฟลเดอร์หลักของเกม (Root Directory) ได้โดยตรง (รองรับเฉพาะ`ฟอนท์ไทย PUA` และ`ฟอนท์ตระกูล JS / PSL`)
- ตั้งค่าฟอนต์ .ttf ไฟล์แรกที่ตรวจเจอให้เป็นฟอนต์เริ่มต้น (Default Font) ของ TextMeshPro (TMP) อัตโนมัติ
- รองรับการสร้าง Font Asset สำหรับ TextMesh Pro
- มีระบบฟอนต์สำรอง (Fallback) ร่วมกับฟอนต์ระบบ (เช่น Tahoma, Microsoft YaHei)
- แก้ปัญหาของ `XUnity.AutoTranslator 5.4.5` ที่ตั้งค่า `OverrideFontTextMeshPro` และ `FallbackFontTextMeshPro` แล้วไม่ทำงาน


## วิธีเช็กประเภทระบบของเกม (Mono หรือ IL2CPP)： 

ตรวจสอบจากไฟล์ภายในโฟลเดอร์หลักของตัวเกม:
- มีไฟล์ `GameAssembly.dll` = ระบบ IL2CPP
- มีโฟลเดอร์ `Managed` และไฟล์ `.dll` อยู่ข้างใน = ระบบ Mono


## ขั้นตอนการติดตั้ง

1. เลือกเวอร์ชันปลั๊กอินให้ตรงกับระบบของเกม `IL2CPP หรือ Mono`
2. นำไฟล์ `TTFLoader-<IL2CPP/Mono>_Thaifont.dll` ที่คอมไพล์แล้ว ไปวางไว้ในโฟลเดอร์ `BepInEx/plugins/`
3. นำไฟล์ฟอนต์ภาษาไทย `.ttf` ไปวางไว้ที่ โฟลเดอร์หลักของเกม (โฟลเดอร์เดียวกับที่อยู่ของไฟล์ .exe เกม)


## รูปแบบการใช้งานและตำแหน่งไฟล์

ปลั๊กอินจะสแกนหาไฟล์ `.ttf` ทั้งหมดในโฟลเดอร์หลักของเกมตอนเปิดเกมขึ้นมา แล้วเลือกใช้ฟอนต์ไฟล์แรกสุดที่เจอ เช่น:
```
โฟลเดอร์เกม/
├── Game.exe
├── BepInEx/
│   └── plugins/
│       └── TTFLoader-<IL2CPP/Mono>_Thaifont.dll
├── JS-Laongdao-Bold.ttf   ← ปลั๊กอินจะดึงฟอนต์นี้ไปใช้งาน
└── other_font.ttf
```


## นามสกุลไฟล์ที่รองรับ
- `.ttf` （ตัวพิมพ์เล็ก）
- `.TTF` （ตัวพิมพ์ใหญ่）


## การดู Log และข้อความการทำงาน

เช็กสถานะการโหลดฟอนต์ได้ที่ไฟล์ `BepInEx/LogOutput.log`

Log ตัวอย่าง：
```
[Info   : TTF Font Loader] Plugin TTF Thai Font Loader is loaded!
[Info   : TTF Font Loader] Successfully set default TMP font to: JS-Laongdao-Bold
```


## ความรองรับ (Compatibility)

- Unity 2021.x
- Unity 2023.x
- BepInEx 6.x (IL2CPP)
- BepInEx 5.x (Mono)
- TextMesh Pro (TMP)

> ⚠️ หมายเหตุ: ยังไม่รองรับ `BepInEx 6.x (Mono)` เนื่องจากตัว `XUnity.AutoTranslator` เองยังไม่รองรับ


## API สำหรับนักพัฒนา (ดึงไปใช้ใน Plugin อื่น)

หากคุณเป็นนักพัฒนาและต้องการโหลดฟอนต์ด้วยตนเอง คุณสามารถใช้เมธอดสาธารณะต่อไปนี้ได้:：

### โหลด Unity Font

```csharp
Font font = TTFLoaderPlugin.Instance.LoadTTF("JS-Laongdao-Bold");
```


### โหลด TMP_FontAsset

```csharp
TMP_FontAsset tmpFont = TTFLoaderPlugin.Instance.LoadTMPTTF("JS-Laongdao-Bold");
```

> ⚠️ หมายเหตุ: วิธีการเหล่านี้จำเป็นต้องตรวจสอบให้แน่ใจว่ามีไฟล์ฟอนต์อยู่ใน root directory ของเกม


## ข้อควรระวังสำคัญ

- หากไม่พบไฟล์ `.ttf` ในโฟลเดอร์หลัก ปลั๊กอินจะไม่ทำการเปลี่ยนฟอนต์ใดๆ
- ปลั๊กอินจะเปลี่ยนฟอนต์เฉพาะเมื่อโหลดไฟล์ใหม่สำเร็จเท่านั้น
- ระบบ IL2CPP: จะสั่งเปลี่ยนไปที่ Default Font ของ TextMesh Pro โดยตรง
- ระบบ Mono: หาก TMP ล้มเหลว จะสลับไปใช้ `UI.Text` ในการโหลดฟอนต์แทน
- ปลั๊กอินนี้เปลี่ยนเฉพาะ ฟอนต์หลักทั้งเกม (Global Default Font) ยังไม่รองรับการเจาะจงเปลี่ยนฟอนต์แยกตามชิ้นส่วน UI
- ตัวปลั๊กอินผ่านการทดสอบจริงบน `Unity BepInEx 6.0.0-be.785 IL2CPP` และ `Unity 2021.3.15f1 BepInEx 5 Mono` เท่านั้น

## Resource
source code repo: https://github.com/you9you/BepInEx-TTFLoaderPlugin
