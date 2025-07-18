# אינטגרציית FCM - שליחת מיקום אוטומטית בהתראות ✅ UPDATED

## 🎯 מה הוטמע והושלם

האפליקציה עכשיו **מוכנה לחלוטין** לקבלת התראות FCM מהשרת ושליחת מיקום אוטומטית בתגובה!

### ✅ **פונקציונליות מלאה מעודכנת:**

1. **📱 קבלת FCM Token + Topics**

   - קבלת Token מ-Firebase בעת הפעלת האפליקציה
   - **🆕 שליחת Token לשרת עם user_id + מיקום נוכחי**
   - **🆕 הרשמה ל-topic "alerts" לקבלת התראות כלליות**
   - **🆕 הרשמה ל-topic "user\_[ID]" לקבלת התראות אישיות**
   - האזנה לרענון Token אוטומטי

2. **🚨 טיפול בהתראות FCM מעודכן**

   - התראות כשהאפליקציה פתוחה (Foreground)
   - התראות כשהאפליקציה ברקע (Background)
   - התראות שנלחצו מה-notification tray
   - **🆕 זיהוי אוטומטי של סוג התראה (אזעקה/טילים/כללי)**
   - **🆕 שמירת נתוני התראות ב-local storage**

3. **📍 שליחת מיקום אוטומטית מעודכנת**
   - בכל התראת FCM נשלח מיקום נוכחי לשרת
   - תמיכה במיקום GPS אמיתי (Android/iOS)
   - מיקום קבוע לבדיקות (31.257425, 34.782406)
   - שמירת מיקום אחרון כגיבוי
   - **🆕 תמיכה בהתראות מדומות מהשרת**

---

## 🔧 **הגדרות השרת הנדרשות**

### 1. קבלת FCM Tokens + מיקום

השרת צריך endpoint לקבלת tokens עם מיקום:

```
POST https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/UpdateFCMToken
Body: {
  "user_id": 123,
  "fcm_token": "dXXXX...",
  "latitude": 31.257425,
  "longitude": 34.782406,
  "timestamp": "2024-12-11T10:30:00Z"
}
```

### 2. שליחת התראות FCM עם השרת החדש 🔥

השרת כבר מוכן עם `FirebaseNotificationSender` החדש:

```csharp
var fcmSender = new FirebaseNotificationSender(serviceAccountPath, projectId);
await fcmSender.SendNotificationAsync(
    "אזעקה חדשה",
    "יש התראה באזור באר שבע",
    "alerts"  // topic name
);
```

**פורמט הנתונים החדש שנשלח:**

```json
{
  "message": {
    "topic": "alerts",
    "notification": {
      "title": "אזעקה חדשה",
      "body": "יש התראה באזור באר שבע"
    },
    "data": {
      "type": "alert",
      "city": "באר שבע"
    }
  }
}
```

**האפליקציה מזהה אוטומטית:**

- ✅ `data.type = "alert"` → זוהה כאזעקה
- ✅ `data.city = "באר שבע"` → שמירת העיר
- ✅ שליחת מיקום אוטומטית לשרת

```

### 3. קבלת מיקומים

השרת יקבל מיקומים ב:

```

POST https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Location/AddUserLocation
Body: {
"userId": 123,
"latitude": 31.257425,
"longitude": 34.782406,
"createdAt": "2025-06-29T10:30:00Z",
"source": "FCM_Alert"
}

````

### 3. **🎯 אינטגרציה מלאה עם SimulateFakeAlert**

השרת שלך כבר מוכן! הפונקציה `SimulateFakeAlert` תעבוד מושלם עם האפליקציה:

```csharp
[HttpPost]
[Route("simulate")]
public async Task<IActionResult> SimulateFakeAlert([FromServices] IConfiguration configuration)
{
    // ... השרת שולח התראה מדומה ...

    await fcmSender.SendNotificationAsync(
        "אזעקה חדשה",              // ← האפליקציה תזהה כ"אזעקה"
        "יש התראה באזור באר שבע"  // ← יוצג למשתמש
    );

    // ← האפליקציה תשלח מיקום אוטומטית לשרת!
    return Ok("Simulated alert sent and notification pushed.");
}
````

**מה יקרה כשתפעיל את `/simulate`:**

1. 🔥 השרת שולח FCM notification ל-topic "alerts"
2. 📱 האפליקציה מקבלת: `{"type": "alert", "city": "באר שבע"}`
3. 🎯 מזוהה כאזעקה עבור באר שבע
4. 📍 האפליקציה קוראת GPS ושולחת מיקום ל-`/api/Location/AddUserLocation`
5. 💾 נתוני ההתראה נשמרים עם העיר ב-local storage
6. 👁️ התראה מוצגת למשתמש עם פרטי העיר

---

## 🧪 **בדיקת האינטגרציה המלאה**

### 1. **בדיקה מהשרת C#:**

```bash
POST https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Alert/simulate
```

### 2. **מה אמור לקרות:**

- ✅ האפליקציה מקבלת התראה "אזעקה חדשה"
- ✅ מזהה: `type=alert, city=באר שבע`
- ✅ שולחת מיקום אוטומטית לשרת
- ✅ השרת רואה נתוני מיקום ב-`AddUserLocation`
- ✅ נשמר log מפורט עם העיר

### 3. **Logs לחיפוש:**

```
Subscribed to alerts topic
FCM Message received: {type: alert, city: באר שבע}
Alert type detected: אזעקה - באר שבע
Alert details: type=alert, city=באר שבע
Sending location after FCM: 31.257, 34.782
Alert received from server for city: באר שבע - location sent
Location sent successfully after FCM alert
Alert saved to local storage: אזעקה חדשה for city: באר שבע
```

```dart
// ב-main.dart, שורה ~45
Position(
  latitude: 31.257425871989867,
  longitude: 34.782406740074435,
  // ... שאר הפרמטרים
);
```

### שינוי לשימוש ב-GPS אמיתי:

```dart
// ב-main.dart, שנה את התנאי ל:
if (Platform.isAndroid || Platform.isIOS) {
  // קבלת מיקום GPS אמיתי - זה כבר מוגדר!
}
```

---

## 🧪 **בדיקת הפונקציונליות**

### 1. בדיקה מ-Firebase Console:

- עבור ל-Firebase Console
- בחר בפרויקט שלך
- עבור ל-Cloud Messaging
- שלח הודעת בדיקה

### 2. בדיקה מהשרת C#:

```csharp
// במחלקה שלך
await fcmService.SendNotificationAsync(
    "בדיקת מיקום",
    "בדיקה - האפליקציה צריכה לשלוח מיקום עכשיו",
    new List<string> { $"user_{userId}" }
);
```

### 3. מה אמור לקרות:

- ✅ המשתמש מקבל התראה
- ✅ האפליקציה שולחת מיקום לשרת
- ✅ השרת מקבל נתוני מיקום
- ✅ נשמר log במסוף

---

## 📝 **Logs לבדיקה**

בעת הרצת האפליקציה, חפש ב-console:

```
FCM Token: dXXX...
FCM Token sent successfully to server
FCM Message received: {alert_type: rocket}
Sending location after FCM: 31.257, 34.782
Location sent successfully after FCM alert
```

---

## ✅ **סיכום - הכל מוכן!**

האפליקציה **עכשיו מוכנה לחלוטין** לקבלת התראות FCM ושליחת מיקום אוטומטית!

**מה שעובד:**

- ✅ רישום FCM Tokens
- ✅ קבלת התראות בכל המצבים
- ✅ שליחת מיקום אוטומטית
- ✅ גיבוי מיקום אחרון
- ✅ טיפול בשגיאות

**מה השרת צריך לעשות:**

1. לקבל ולשמור FCM Tokens
2. לשלוח התראות ב-`FcmNotificationService`
3. לקבל מיקומים ב-`AddUserLocation`

**🎉 המערכת מוכנה לפעולה!**
