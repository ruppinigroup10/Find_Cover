import 'dart:convert';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;
import 'firebase_options.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:flutter/material.dart';
import 'navigation_map_new.dart';
import 'main.dart' show navigatorKey;

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

    // **עדיפות ראשונה: נסה לקבל מיקום חדש (אם אפשר)**
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
      }
    } else {
      print("❌ Location service disabled");
    }

    // **אם לא הצלחנו לקבל מיקום חדש - ננסה מיקום אחרון**
    if (position == null) {
      try {
        print("🎯 [$timeString] Getting last known position (fallback)...");
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
          } else {
            print(
              "⏰ [$timeString] Last known location is old (${hoursDiff}h), but using as fallback...",
            );
          }
        } else {
          print("❌ [$timeString] No last known position available");
        }
      } catch (e) {
        print("⚠️ [$timeString] Could not get last known position: $e");
      }
    }

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

    // בדיקת קישוריות לשרת לפני שליחת הבקשה
    print(
      "🔍 [$timeString] Checking server connectivity (but continuing anyway)...",
    );
    try {
      await testServerConnectivity();
    } catch (e) {
      print("⚠️ [$timeString] Connectivity test failed, but continuing: $e");
    }

    final payload = {
      "userId": int.tryParse(userId) ?? 0,
      "latitude": position.latitude,
      "longitude": position.longitude,
      "timestamp": DateTime.now().toUtc().toIso8601String(),
    };

    // בדיקות נוספות לפני שליחה
    const String actualUrl =
        "https://proj.ruppin.ac.il/igroup10/test2/tar1/api/EmergencyResponse/get-shelter-route";

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
    // print("📝 [$timeString] Response body length: ${response.body.length}");
    print("🔍 [$timeString] === END SERVER RESPONSE ANALYSIS ===");

    if (response.statusCode == 200) {
      print("✅ [$timeString] Silent location verification sent successfully");
      // print("📋 [$timeString] Response body: ${response.body}");

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

  // הגדרת הרשאות לקבלת התראות חזותיות מלאות
  await FirebaseMessaging.instance.requestPermission(
    alert: true, // להציג התראות חזותיות
    announcement: true,
    badge: true, // להציג badge
    carPlay: true,
    criticalAlert: true,
    provisional: false,
    sound: true, // להשמיע צלילים
  );

  print("� FCM configured for FULL VISUAL notifications");
  print("📱 Notifications will appear expanded and visible");

  // רישום ל-topic של התראות
  try {
    await FirebaseMessaging.instance.subscribeToTopic("alerts");
    print("✅ Successfully subscribed to 'alerts' topic");
  } catch (e) {
    print("❌ Failed to subscribe to 'alerts' topic: $e");
  }

  // קבלת FCM token לדיבוג
  try {
    String? token = await FirebaseMessaging.instance.getToken();
    print("📱 FCM Token: $token");
  } catch (e) {
    print("❌ Failed to get FCM token: $e");
  }

  // טיפול בהודעות כשהאפליקציה פתוחה - הצגת באנר
  FirebaseMessaging.onMessage.listen((RemoteMessage message) {
    print("📩 === FCM MESSAGE WHILE APP OPEN ===");
    print("📱 Title: ${message.notification?.title ?? 'No title'}");
    print("📱 Body: ${message.notification?.body ?? 'No body'}");
    print("📊 Data: ${message.data}");
    print("🔍 Data keys: ${message.data.keys.toList()}");
    print("🔍 Data values: ${message.data.values.toList()}");

    // הצגת באנר התראה בחלק העליון של המסך
    _showBannerNotification(message);

    // בדיקה אם זה הודעת אזעקה - נווט למסך ניווט
    if (message.data['code'] == 'FIND_SHELTER' ||
        message.data['type'] == 'trigger_location' ||
        message.data['type'] == 'alert' ||
        (message.notification?.title == "נמצא עבורך מרחב מוגן") ||
        (message.notification?.title == "התקבלה אזעקה")) {
      print("🎯 Alert with shelter location detected! Navigating to map...");
      _navigateToNavigationMap(message.data);
    } else {
      print("ℹ️ No matching trigger found, processing in background");
      _sendLocationToServer();
    }
  });

  // טיפול בלחיצה על הודעה - ניווט למסך ניווט
  FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
    print("👆 === FCM MESSAGE CLICKED ===");
    print("📊 Data: ${message.data}");
    print("🔍 Data keys: ${message.data.keys.toList()}");
    print("🔍 Data values: ${message.data.values.toList()}");
    print("📱 Title: ${message.notification?.title ?? 'No title'}");
    print("📱 Body: ${message.notification?.body ?? 'No body'}");

    // נווט למסך ניווט בכל מקרה אם זה הודעת אזעקה
    if (message.data['code'] == 'FIND_SHELTER' ||
        message.data['type'] == 'trigger_location' ||
        message.data['type'] == 'alert' ||
        (message.notification?.title == "נמצא עבורך מרחב מוגן") ||
        (message.notification?.title == "התקבלה אזעקה")) {
      print("🎯 Alert clicked! Navigating to map with shelter location...");
      _navigateToNavigationMap(message.data);
    } else {
      print("ℹ️ No matching trigger found in clicked message");
      // גם אם אין טריגר מזוהה, עדיין נווט למסך ניווט
      print("🔄 Navigating to map anyway...");
      _navigateToNavigationMap(message.data);
    }
  });

  print("✅ SILENT FCM listeners initialized successfully");
  print(
    "🔇 All alert processing will be done in background without user notifications",
  );

  // בדיקת מצב FCM לדיבוג
  await testFCMStatus();
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
          .timeout(const Duration(seconds: 5)); // קצר יותר
      print("✅ [$timeString] Response: ${response.statusCode} for $url");

      // אם זה 405 - זה בסדר, זה אומר שהשרת קיים אבל לא מקבל GET
      if (response.statusCode == 405) {
        print("ℹ️ [$timeString] 405 is OK - server exists but expects POST");
      }
    } catch (e) {
      print("❌ [$timeString] Failed: $e for $url");
    }
  }

  print("🔍 [$timeString] === END SERVER CONNECTIVITY TEST ===");
}

/// בדיקת מצב FCM והרשאות
Future<void> testFCMStatus() async {
  print("🔍 === TESTING FCM STATUS ===");

  try {
    // בדיקת token
    String? token = await FirebaseMessaging.instance.getToken();
    print("📱 FCM Token: $token");

    // בדיקת הרשאות
    NotificationSettings settings =
        await FirebaseMessaging.instance.getNotificationSettings();
    print("📱 Authorization Status: ${settings.authorizationStatus}");
    print("🔔 Alert Permission: ${settings.alert}");
    print("🔊 Sound Permission: ${settings.sound}");
    print("🎯 Badge Permission: ${settings.badge}");

    // בדיקת topic subscription
    print("📝 Checking topic subscription...");
    // לא ניתן לבדוק ישירות אבל נוכל לראות אם יש שגיאות
  } catch (e) {
    print("❌ Error checking FCM status: $e");
  }

  print("🔍 === END FCM STATUS TEST ===");
}

/// ניווט למסך הניווט עם פרטי המרחב המוגן
void _navigateToNavigationMap(Map<String, dynamic> data) {
  print("🗺️ === NAVIGATING TO MAP WITH SHELTER DATA ===");
  print("📊 Received data: $data");

  // חיפוש ה-context הנוכחי
  final BuildContext? context = _getAppContext();

  if (context == null) {
    print("❌ No context available for navigation");
    print("🔄 Trying to navigate after delay...");

    // נסיון נוסף עם דיליי
    Future.delayed(const Duration(milliseconds: 500), () {
      final retryContext = _getAppContext();
      if (retryContext != null) {
        _navigateToNavigationMap(data);
      } else {
        print("❌ Still no context after retry");
      }
    });
    return;
  }

  // יצירת פרטי המרחב המוגן מהדאטה
  Map<String, dynamic> shelterDetails = {};

  // אם יש מיקום בדאטה
  if (data.containsKey('latitude') && data.containsKey('longitude')) {
    shelterDetails = {
      'latitude': double.tryParse(data['latitude'].toString()) ?? 0.0,
      'longitude': double.tryParse(data['longitude'].toString()) ?? 0.0,
      'name': data['shelter_name'] ?? 'מרחב מוגן',
      'address': data['address'] ?? '',
      'distance': data['distance'] ?? '',
    };
    print("✅ Using shelter data from FCM: $shelterDetails");
  } else {
    print("⚠️ No location in FCM data, trying local storage...");
    // אם אין מיקום בדאטה, נחפש בשמירה מקומית
    _loadShelterFromLocalStorage().then((savedShelter) {
      if (savedShelter != null) {
        shelterDetails = savedShelter;
        print("✅ Using shelter data from local storage: $shelterDetails");
        _performNavigation(context, shelterDetails);
      } else {
        print("❌ No shelter location available anywhere");
        // גם אם אין מיקום, עדיין נווט למסך ניווט
        _performNavigation(context, {});
      }
    });
    return;
  }

  _performNavigation(context, shelterDetails);
}

/// ביצוע הניווט בפועל
void _performNavigation(
  BuildContext context,
  Map<String, dynamic> shelterDetails,
) {
  print("🚀 === PERFORMING NAVIGATION ===");
  print("📊 Shelter details: $shelterDetails");
  print("🔍 Context: $context");
  print("🔍 Context mounted: ${context.mounted}");

  try {
    // אם כבר נמצאים במסך ניווט, החלף אותו
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(
        builder: (context) => NavigationMapPage(shelterDetails: shelterDetails),
      ),
    );
    print("✅ Navigation completed successfully");
  } catch (e) {
    print("❌ Navigation failed: $e");
    print("🔍 Error details: ${e.toString()}");

    // נסיון עם push רגיל במקום pushReplacement
    try {
      Navigator.of(context).push(
        MaterialPageRoute(
          builder:
              (context) => NavigationMapPage(shelterDetails: shelterDetails),
        ),
      );
      print("✅ Navigation with push completed successfully");
    } catch (e2) {
      print("❌ Navigation with push also failed: $e2");
    }
  }
}

/// קבלת ה-context הנוכחי של האפליקציה
BuildContext? _getAppContext() {
  print("🔍 === GETTING APP CONTEXT ===");

  // השתמש ב-NavigatorKey הגלובלי
  final context = navigatorKey.currentContext;
  if (context != null) {
    print("✅ Got context from navigatorKey");
    print("🔍 Context type: ${context.runtimeType}");
    print("🔍 Context widget: ${context.widget}");
    return context;
  }

  print("❌ NavigatorKey context is null");
  print("🔍 NavigatorKey state: ${navigatorKey.currentState}");
  print("🔍 NavigatorKey mounted: ${navigatorKey.currentState?.mounted}");

  return null;
}

/// טעינת מרחב מוגן מאחסון מקומי
Future<Map<String, dynamic>?> _loadShelterFromLocalStorage() async {
  try {
    final prefs = await SharedPreferences.getInstance();
    String? shelterData = prefs.getString('shelter_route_data');

    if (shelterData != null) {
      Map<String, dynamic> data = jsonDecode(shelterData);
      if (data['shelterDetails'] != null) {
        return data['shelterDetails'];
      }
    }

    return null;
  } catch (e) {
    print("❌ Error loading shelter from storage: $e");
    return null;
  }
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

/// הצגת באנר התראה בחלק העליון של המסך כשהאפליקציה פתוחה
void _showBannerNotification(RemoteMessage message) {
  final context = navigatorKey.currentContext;
  if (context == null) {
    print("❌ No context available for banner notification");
    return;
  }

  final title = message.notification?.title ?? "התראה";
  final body = message.notification?.body ?? "הודעה חדשה התקבלה";

  print("📢 Showing banner notification: $title - $body");

  // הצגת MaterialBanner בחלק העליון של המסך
  ScaffoldMessenger.of(context).showMaterialBanner(
    MaterialBanner(
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontWeight: FontWeight.bold,
              fontSize: 16,
              color: Colors.white,
            ),
          ),
          const SizedBox(height: 4),
          Text(body, style: const TextStyle(fontSize: 14, color: Colors.white)),
        ],
      ),
      backgroundColor: Colors.red[700], // צבע אדום לאזעקה
      leading: const Icon(Icons.warning, color: Colors.white, size: 32),
      actions: [
        TextButton(
          onPressed: () {
            ScaffoldMessenger.of(context).hideCurrentMaterialBanner();
          },
          child: const Text("סגור", style: TextStyle(color: Colors.white)),
        ),
        TextButton(
          onPressed: () {
            ScaffoldMessenger.of(context).hideCurrentMaterialBanner();
            _navigateToNavigationMap(message.data);
          },
          child: const Text(
            "ניווט",
            style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
          ),
        ),
      ],
    ),
  );

  // הסתרה אוטומטית אחרי 10 שניות
  Future.delayed(const Duration(seconds: 10), () {
    try {
      ScaffoldMessenger.of(context).hideCurrentMaterialBanner();
    } catch (e) {
      print("Banner already hidden: $e");
    }
  });
}
