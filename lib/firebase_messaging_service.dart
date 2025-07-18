import 'dart:convert';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;
import 'firebase_options.dart';
import 'package:shared_preferences/shared_preferences.dart';

// טיפול בהתראות ברקע - ציבורי לשימוש ב-main.dart
@pragma('vm:entry-point') // זה חובה לפונקציה ברקע
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);

  print("🔥 === FCM SILENT BACKGROUND MESSAGE RECEIVED ===");
  print("📱 Title: ${message.notification?.title ?? 'No title'}");
  print("📱 Body: ${message.notification?.body ?? 'No body'}");
  print("📊 Data: ${message.data}");
  print("🔍 Looking for alert trigger...");

  final data = message.data;
  // שלח מיקום תמיד כשמתקבלת התראת אזעקה - ללא תלות באזור המשתמש
  if (data['code'] == 'FIND_SHELTER' ||
      data['type'] == 'trigger_location' ||
      data['type'] == 'alert' ||
      (message.notification?.title == "התקבלה אזעקה")) {
    print(
      "🎯 SILENT Alert trigger detected! Sending location to verify user area...",
    );
    print(
      "🔇 This will NOT show any notification to user - background verification only",
    );
    await _sendLocationToServer();
  } else {
    print("ℹ️ No alert trigger found, message ignored silently");
  }

  print("✅ Silent background message processing completed");
}

// שליחת מיקום לשרת - אופטימיזציה למהירות ברקע
Future<void> _sendLocationToServer() async {
  try {
    final now = DateTime.now();
    final timeString =
        "${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}:${now.second.toString().padLeft(2, '0')}";

    print("🔍 === [$timeString] SILENT LOCATION VERIFICATION FOR ALERT ===");
    print("🔇 This is a silent background process - no user notifications");

    // בדיקת הרשאות
    LocationPermission permission = await Geolocator.checkPermission();
    print("🔧 [$timeString] Location permission: $permission");

    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      print(
        "❌ [$timeString] Location permission denied - cannot verify user area",
      );
      return;
    }

    Position? position;

    // **עדיפות ראשונה: מיקום אחרון (הכי מהיר ואמין ברקע)**
    try {
      print(
        "🎯 [$timeString] Getting last known position (fastest for background)...",
      );
      position = await Geolocator.getLastKnownPosition();
      if (position != null) {
        // בדוק אם המיקום לא ישן מדי
        final now = DateTime.now();
        final locationTime = position.timestamp;
        final hoursDiff = now.difference(locationTime).inHours;

        if (hoursDiff < 6) {
          // פחות מ-6 שעות (מספיק טוב לחירום)
          print(
            "📍 [$timeString] Using last known location (${hoursDiff}h old): ${position.latitude}, ${position.longitude}",
          );
          await _sendLocationData(position);
          return; // נצא מהפונקציה - שלחנו בהצלחה
        } else {
          print(
            "⏰ [$timeString] Last known location is old (${hoursDiff}h), will try fresh...",
          );
        }
      } else {
        print("❌ [$timeString] No last known position available");
      }
    } catch (e) {
      print("⚠️ [$timeString] Could not get last known position: $e");
    }

    // **רק אם אין מיקום אחרון או שהוא ישן מדי - נסה מיקום חדש**
    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    print("🔧 Location service enabled: $serviceEnabled");

    if (serviceEnabled) {
      try {
        print("🎯 Attempting fresh location (background timeout: 3 sec)...");

        // הגדרות מותאמות לרקע
        position = await Geolocator.getCurrentPosition(
          locationSettings: const LocationSettings(
            accuracy: LocationAccuracy.low, // דיוק נמוך = מהירות גבוהה
            timeLimit: Duration(seconds: 3), // timeout קצר מאוד ברקע
          ),
        ).timeout(const Duration(seconds: 5)); // timeout כפול בטיחות

        print("📍 Fresh location: ${position.latitude}, ${position.longitude}");
      } catch (e) {
        print("⚠️ Fresh location timeout/failed (expected in background): $e");

        // **גיבוי אחרון: כל מיקום אחרון שיש**
        try {
          position = await Geolocator.getLastKnownPosition();
          if (position != null) {
            print(
              "📍 Fallback to any last known location: ${position.latitude}, ${position.longitude}",
            );
          } else {
            print("❌ No location data available at all");
            return;
          }
        } catch (e2) {
          print("❌ All location methods failed: $e2");
          return;
        }
      }
    } else {
      print("❌ Location service disabled");
      // גם אם השירות כבוי, נסה מיקום אחרון
      if (position == null) {
        try {
          position = await Geolocator.getLastKnownPosition();
          if (position != null) {
            print(
              "📍 Using last known location despite disabled service: ${position.latitude}, ${position.longitude}",
            );
          }
        } catch (e) {
          print("❌ Cannot get any location data");
          return;
        }
      }
    }

    // שלח את המיקום לשרת
    if (position != null) {
      await _sendLocationData(position);
    } else {
      print("❌ No position data to send");
    }
  } catch (e) {
    print("⚠ שגיאה כללית בשליחת מיקום: $e");
  }
}

// פונקציה נפרדת לשליחת נתוני המיקום לשרת
Future<void> _sendLocationData(Position position) async {
  try {
    final now = DateTime.now();
    final timeString =
        "${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}:${now.second.toString().padLeft(2, '0')}";

    // קבלת מזהה המשתמש
    String userId = await getUserIdFromStorage();
    print("👤 [$timeString] User ID: $userId");

    if (userId.isEmpty) {
      print("❌ [$timeString] User ID not found, cannot send location");
      return;
    }

    print(
      "🔧 [$timeString] Using API endpoint: https://proj.ruppin.ac.il/igroup10/test2/tar1/api/EmergencyResponse/get-shelter-route",
    );

    // בדיקת קישוריות לשרת לפני שליחת הבקשה
    await testServerConnectivity();

    // בדיקות נוספות לדיבוג
    print("🔍 [$timeString] === URL VERIFICATION ===");
    const String serverUrl =
        "https://proj.ruppin.ac.il/igroup10/test2/tar1/api/EmergencyResponse/get-shelter-route";
    print("🔗 [$timeString] Hardcoded URL: $serverUrl");
    print(
      "📝 [$timeString] URL contains 'igroup10': ${serverUrl.contains('igroup10')}",
    );
    print(
      "📝 [$timeString] URL contains 'group10': ${serverUrl.contains('group10')}",
    );
    print("📝 [$timeString] URL length: ${serverUrl.length}");
    print("🔍 [$timeString] === END URL VERIFICATION ===");

    final payload = {
      "userId": int.tryParse(userId) ?? 0,
      "latitude": position.latitude,
      "longitude": position.longitude,
      "timestamp": DateTime.now().toUtc().toIso8601String(),
    };

    print(
      "📦 [$timeString] SILENT location verification - sending to server: ${jsonEncode(payload)}",
    );
    print(
      "🌐 [$timeString] Server URL: https://proj.ruppin.ac.il/igroup10/test2/tar1/api/EmergencyResponse/get-shelter-route",
    );

    // בדיקות נוספות לפני שליחה
    const String actualUrl =
        "https://proj.ruppin.ac.il/igroup10/test2/tar1/api/EmergencyResponse/get-shelter-route";
    print("🔍 [$timeString] === HTTP REQUEST VERIFICATION ===");
    print("🌐 [$timeString] About to POST to: $actualUrl");
    print(
      "📤 [$timeString] Request headers: {'Content-Type': 'application/json'}",
    );
    print("📦 [$timeString] Request body: ${jsonEncode(payload)}");
    print("⏱️ [$timeString] Request timeout: 30 seconds");
    print("🔍 [$timeString] === END HTTP REQUEST VERIFICATION ===");

    final response = await http
        .post(
          Uri.parse(actualUrl),
          headers: {"Content-Type": "application/json"},
          body: jsonEncode(payload),
        )
        .timeout(const Duration(seconds: 30)); // timeout לבקשת HTTP

    print("🔍 [$timeString] === SERVER RESPONSE ANALYSIS ===");
    print("📥 [$timeString] Server response: ${response.statusCode}");
    print("📋 [$timeString] Response headers: ${response.headers}");
    print("📝 [$timeString] Response body length: ${response.body.length}");

    // נבדוק אם השרת החזיר HTML במקום JSON (טיפוסי ל-404)
    if (response.body.contains('<html>') ||
        response.body.contains('<!DOCTYPE')) {
      print(
        "🚨 [$timeString] Response is HTML (not JSON) - indicating 404 or server error page",
      );
      print(
        "📄 [$timeString] HTML response preview: ${response.body.substring(0, response.body.length > 200 ? 200 : response.body.length)}",
      );
    } else {
      print("📋 [$timeString] Response body: ${response.body}");
    }
    print("🔍 [$timeString] === END SERVER RESPONSE ANALYSIS ===");

    if (response.statusCode == 200) {
      print("✅ [$timeString] Silent location verification sent successfully");
      print("📋 [$timeString] Response body: ${response.body}");

      // פענוח התגובה מהשרת
      try {
        final responseData = jsonDecode(response.body);
        print("📋 [$timeString] Server response data: $responseData");

        // אם השרת מחזיר נתיב למרחב מוגן, שמור את הנתונים
        if (responseData['success'] == true &&
            responseData['requiresAction'] == true) {
          print(
            "🚨 [$timeString] User IS in alert area - shelter route required!",
          );
          await _saveShelterRouteData(responseData);
        } else {
          print(
            "✅ [$timeString] User is NOT in alert area - no action needed (silent)",
          );
        }
      } catch (jsonError) {
        print("⚠️ [$timeString] Failed to parse server response: $jsonError");
      }
    } else {
      print(
        "❌ [$timeString] Silent location verification failed: ${response.statusCode}",
      );

      // אל תדפיס את כל ה-HTML אם זה שגיאת 404
      if (response.statusCode == 404) {
        print("🔍 [$timeString] === 404 ERROR ANALYSIS ===");
        print(
          "📥 [$timeString] Server endpoint not found (404) - check API URL",
        );
        print(
          "🔧 [$timeString] Expected URL: https://proj.ruppin.ac.il/igroup10/test2/tar1/api/EmergencyResponse/get-shelter-route",
        );
        print(
          "📱 [$timeString] Make sure the API endpoint exists on the server",
        );

        // בדיקות נוספות
        print("🔍 [$timeString] === POSSIBLE CAUSES ===");
        print("1️⃣ [$timeString] URL path might be wrong");
        print("2️⃣ [$timeString] Server folder structure might be different");
        print("3️⃣ [$timeString] API endpoint might not be deployed");
        print("4️⃣ [$timeString] Server might be down or misconfigured");

        // נחליק את ה-URL לחלקים
        print("🔍 [$timeString] === URL BREAKDOWN ===");
        print("🌐 [$timeString] Domain: proj.ruppin.ac.il");
        print("📁 [$timeString] Project path: /igroup10/test2/tar1");
        print(
          "🛠️ [$timeString] API path: /api/EmergencyResponse/get-shelter-route",
        );
        print("📝 [$timeString] HTTP Method: POST");
        print("🔍 [$timeString] === END 404 ANALYSIS ===");
      } else if (response.statusCode == 500) {
        print("📥 [$timeString] Server internal error (500)");
        print("🔧 [$timeString] Check server logs for more details");
      } else {
        // רק עבור שגיאות אחרות, הדפס את התחילית של התגובה
        String responsePreview =
            response.body.length > 200
                ? response.body.substring(0, 200) + "..."
                : response.body;
        print("📥 [$timeString] Error response preview: $responsePreview");
      }
    }
  } catch (e) {
    final now = DateTime.now();
    final timeString =
        "${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}:${now.second.toString().padLeft(2, '0')}";
    print("⚠️ [$timeString] Network/Connection error: ${e.toString()}");
    print("🔧 [$timeString] Check internet connection and server URL");
  }
}

// פונקציה לשליפת userId מהאחסון המקומי
Future<String> getUserIdFromStorage() async {
  try {
    final prefs = await SharedPreferences.getInstance();
    String? userId = prefs.getString('user_id');

    if (userId == null || userId.isEmpty) {
      print("❌ User ID not found in SharedPreferences");
      return '';
    }

    print("✅ User ID found: $userId");
    return userId;
  } catch (e) {
    print("❌ Error getting user ID: $e");
    return '';
  }
}

// שמירת נתוני נתיב מרחב מוגן
Future<void> _saveShelterRouteData(Map<String, dynamic> routeData) async {
  try {
    final prefs = await SharedPreferences.getInstance();

    // שמירת נתוני המרחב המוגן
    if (routeData['shelterDetails'] != null) {
      final shelterDetails = routeData['shelterDetails'];
      await prefs.setString('shelter_route_data', jsonEncode(routeData));
      await prefs.setString(
        'current_shelter_id',
        shelterDetails['shelterId'].toString(),
      );
      await prefs.setString(
        'current_shelter_name',
        shelterDetails['name'] ?? '',
      );
      await prefs.setDouble(
        'current_shelter_lat',
        shelterDetails['latitude']?.toDouble() ?? 0.0,
      );
      await prefs.setDouble(
        'current_shelter_lng',
        shelterDetails['longitude']?.toDouble() ?? 0.0,
      );

      // שמירת זמן קבלת המיקום מהשרת
      await prefs.setString(
        'shelter_received_time',
        DateTime.now().toIso8601String(),
      );

      final now = DateTime.now();
      final timeString =
          "${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}:${now.second.toString().padLeft(2, '0')}";

      print("✅ [$timeString] Shelter route data saved from server:");
      print("📍 [$timeString] Name: ${shelterDetails['name']}");
      print(
        "📍 [$timeString] Location: ${shelterDetails['latitude']}, ${shelterDetails['longitude']}",
      );
      print("🗺️ [$timeString] This location will be used for navigation");
    }
  } catch (e) {
    print("❌ Error saving shelter route data: $e");
  }
}

// את זה תריץ ב-main.dart
Future<void> initializeFCM() async {
  print("🚀 Initializing SILENT FCM listeners...");
  print("🔇 All alerts will be processed silently in background");

  // הגדרת handler לרקע
  FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);

  // הגדרת הרשאות לקבלת התראות (אבל לא להציג אותן)
  await FirebaseMessaging.instance.requestPermission(
    alert: false, // לא להציג התראות חזותיות
    announcement: false,
    badge: false, // לא להציג badge
    carPlay: false,
    criticalAlert: false,
    provisional: false,
    sound: false, // לא להשמיע צלילים
  );

  print("🔇 FCM configured for SILENT operation only");

  // טיפול בהודעות כשהאפליקציה פתוחה - התראות שקטות בלבד
  FirebaseMessaging.onMessage.listen((RemoteMessage message) {
    print("📩 === FCM SILENT MESSAGE WHILE APP OPEN ===");
    print("📱 Title: ${message.notification?.title ?? 'No title'}");
    print("📱 Body: ${message.notification?.body ?? 'No body'}");
    print("📊 Data: ${message.data}");

    // בדיקה אם זה הודעת אזעקה - תמיד שלח מיקום ברקע (שקט)
    if (message.data['code'] == 'FIND_SHELTER' ||
        message.data['type'] == 'trigger_location' ||
        message.data['type'] == 'alert' ||
        (message.notification?.title == "התקבלה אזעקה")) {
      print("🎯 Silent alert detected! Sending location silently...");
      print(
        "🔇 This alert will NOT be shown to user - silent background trigger only",
      );
      _sendLocationToServer();

      // לא מציגים שום התראה למשתמש - עובד רק ברקע
      print("✅ Silent location trigger completed - no user notification");
    } else {
      print("ℹ️ No matching trigger found, message ignored silently");
    }
  });

  // טיפול בלחיצה על הודעה - גם זה יהיה שקט
  FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
    print("👆 === FCM SILENT MESSAGE CLICKED ===");
    print("📊 Data: ${message.data}");

    if (message.data['code'] == 'FIND_SHELTER' ||
        message.data['type'] == 'trigger_location' ||
        message.data['type'] == 'alert' ||
        (message.notification?.title == "התקבלה אזעקה")) {
      print("🎯 Silent alert clicked! Sending location verification...");
      print("🔇 Processing silently in background...");
      _sendLocationToServer();
      // לא מציגים שום UI נוסף - הכל ברקע
    }
  });

  print("✅ SILENT FCM listeners initialized successfully");
  print(
    "🔇 All alert processing will be done in background without user notifications",
  );
}

// פונקציה ציבורית לשליחת מיקום לשרת עבור FCM
Future<void> sendLocationToServerFromFCM() async {
  final now = DateTime.now();
  final timeString =
      "${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}:${now.second.toString().padLeft(2, '0')}";
  print("📞 [$timeString] Manual FCM location trigger called");
  await _sendLocationToServer();
}

/// בדיקת קישוריות לשרת
Future<void> testServerConnectivity() async {
  final now = DateTime.now();
  final timeString =
      "${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}:${now.second.toString().padLeft(2, '0')}";

  print("🔍 [$timeString] === TESTING SERVER CONNECTIVITY ===");

  // נבדוק קישורים שונים לאימות
  final List<String> urlsToTest = [
    "https://proj.ruppin.ac.il/igroup10/test2/tar1/api/EmergencyResponse/get-shelter-route",
    "https://proj.ruppin.ac.il/group10/test2/tar1/api/EmergencyResponse/get-shelter-route", // URL הישן
    "https://proj.ruppin.ac.il/igroup10/test2/tar1/", // בסיס האתר
    "https://proj.ruppin.ac.il/igroup10/", // בסיס הפרויקט
  ];

  for (String url in urlsToTest) {
    try {
      print("🌐 [$timeString] Testing URL: $url");
      final response = await http
          .get(Uri.parse(url))
          .timeout(const Duration(seconds: 10));
      print("✅ [$timeString] Response: ${response.statusCode} for $url");
    } catch (e) {
      print("❌ [$timeString] Failed: $e for $url");
    }
  }

  print("🔍 [$timeString] === END SERVER CONNECTIVITY TEST ===");
}

/// בדיקת מתודת HTTP נכונה
Future<void> testHttpMethods(String url) async {
  final now = DateTime.now();
  final timeString =
      "${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}:${now.second.toString().padLeft(2, '0')}";

  print("🔍 [$timeString] === TESTING HTTP METHODS ===");

  // בדיקת GET
  try {
    final getResponse = await http
        .get(Uri.parse(url))
        .timeout(const Duration(seconds: 10));
    print("🟢 [$timeString] GET $url: ${getResponse.statusCode}");
  } catch (e) {
    print("🔴 [$timeString] GET $url: Failed - $e");
  }

  // בדיקת POST בלי body
  try {
    final postResponse = await http
        .post(Uri.parse(url))
        .timeout(const Duration(seconds: 10));
    print("🟡 [$timeString] POST $url (no body): ${postResponse.statusCode}");
  } catch (e) {
    print("🔴 [$timeString] POST $url (no body): Failed - $e");
  }

  // בדיקת POST עם headers בלבד
  try {
    final postHeadersResponse = await http
        .post(Uri.parse(url), headers: {"Content-Type": "application/json"})
        .timeout(const Duration(seconds: 10));
    print(
      "🟠 [$timeString] POST $url (headers only): ${postHeadersResponse.statusCode}",
    );
  } catch (e) {
    print("🔴 [$timeString] POST $url (headers only): Failed - $e");
  }

  print("🔍 [$timeString] === END HTTP METHODS TEST ===");
}
