/*
import 'Enter.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:geolocator/geolocator.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'local_storage_service.dart';
import 'home_page.dart';
import 'dart:async';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:firebase_messaging/firebase_messaging.dart' show RemoteMessage;
import 'package:firebase_core/firebase_core.dart';
import 'dart:developer';
import 'firebase_messaging_service.dart';
import 'package:background_location_tracker/background_location_tracker.dart'
    as bg_tracker;
// Google Maps imports for proper initialization
import 'package:google_maps_flutter_android/google_maps_flutter_android.dart';
import 'package:google_maps_flutter_platform_interface/google_maps_flutter_platform_interface.dart';
final RouteObserver<PageRoute> routeObserver = RouteObserver<PageRoute>();
// Background callback for location tracking
@pragma('vm:entry-point')
void backgroundCallback() {
  bg_tracker.BackgroundLocationTrackerManager.handleBackgroundUpdated((
    data,
  ) async {
    log(
      '[BACKGROUND] Location update received: lat=${data.lat}, lon=${data.lon}',
    );
    // You can add additional logic here if needed
  });
}
Future<void> ensureLocationPermissionAndService(BuildContext context) async {
  final prefs = await SharedPreferences.getInstance();
  if (prefs.getBool('locationDialogClosed') == true) {
    return;
  }
  bool permissionGranted = false;
  bool serviceEnabled = false;
  while (!permissionGranted || !serviceEnabled) {
    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      await Geolocator.requestPermission();
    }
    // Request always permission
    if (permission != LocationPermission.always) {
      permission = await Geolocator.requestPermission();
    }
    permissionGranted = permission == LocationPermission.always;
    serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!permissionGranted) {
      await showDialog(
        context: context,
        barrierDismissible: false,
        builder:
            (ctx) => AlertDialog(
              title: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Expanded(
                    child: Text(
                      'הרשאת מיקום נדרשת',
                      textAlign: TextAlign.center,
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    tooltip: 'סגור',
                    onPressed: () async {
                      await prefs.setBool('locationDialogClosed', true);
                      Navigator.of(ctx).pop();
                    },
                  ),
                ],
              ),
              content: const Text(
                'כדי שהאפליקציה תוכל לשלוח מיקום גם ברקע, יש לאשר "גישה למיקום כל הזמן" (Always).\n\nלאחר לחיצה על "פתח הגדרות":\n1. לחץ על "הרשאות"\n2. בחר "מיקום"\n3. סמן "כן, כל הזמן"\n\nבנוסף, מומלץ להחריג את האפליקציה ממנגנון חיסכון סוללה (Battery Optimization) כדי להבטיח שליחת מיקום גם כשהאפליקציה עובדת ברקע. ניתן לעשות זאת דרך: הגדרות > סוללה > חיסכון סוללה > החרג את האפליקציה.',
                textDirection: TextDirection.rtl,
                textAlign: TextAlign.center,
              ),
              actionsAlignment: MainAxisAlignment.center,
              actions: [
                TextButton(
                  onPressed: () async {
                    Navigator.of(ctx).pop();
                    await Geolocator.openAppSettings();
                  },
                  child: const Text('פתח הגדרות', textAlign: TextAlign.center),
                ),
              ],
            ),
      );
      await Future.delayed(const Duration(seconds: 1));
      if (prefs.getBool('locationDialogClosed') == true) {
        return;
      }
    } else if (!serviceEnabled) {
      await Geolocator.openLocationSettings();
      await Future.delayed(const Duration(seconds: 2));
    }
  }
}
Future<void> showLocationPermissionFlow(BuildContext context) async {
  final prefs = await SharedPreferences.getInstance();
  if (prefs.getBool('dontAskLocationDialog') == true) {
    return;
  }
  bool dontAskAgain = false;
  final bool? wantsLastLocation = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (ctx) {
      return StatefulBuilder(
        builder:
            (ctx, setState) => AlertDialog(
              title: const Text(
                'שימוש במיקום אחרון',
                textAlign: TextAlign.center,
              ),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text(
                    'האם תרצה לקבל מענה על בסיס מיקום אחרון במקרה שלא תהיה קליטה באזעקה?',
                    textDirection: TextDirection.rtl,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Checkbox(
                        value: dontAskAgain,
                        onChanged: (val) {
                          setState(() {
                            dontAskAgain = val ?? false;
                          });
                        },
                      ),
                      Text('אל תשאל אותי שוב'),
                    ],
                  ),
                ],
              ),
              actionsAlignment: MainAxisAlignment.center,
              actions: [
                TextButton(
                  onPressed: () {
                    if (dontAskAgain) {
                      prefs.setBool('dontAskLocationDialog', true);
                    }
                    Navigator.of(ctx).pop(true);
                  },
                  child: const Text('כן'),
                ),
                TextButton(
                  onPressed: () {
                    if (dontAskAgain) {
                      prefs.setBool('dontAskLocationDialog', true);
                    }
                    Navigator.of(ctx).pop(false);
                  },
                  child: const Text('לא'),
                ),
              ],
            ),
      );
    },
  );
  if (wantsLastLocation == true) {
    await Geolocator.openAppSettings();
    await showDialog(
      context: context,
      barrierDismissible: false,
      builder:
          (ctx) => AlertDialog(
            title: const Text('שמירה על פרטיות', textAlign: TextAlign.center),
            content: const Text(
              'אנו מתחייבים לשמירה על פרטיותכם ושומרים מיקום אחרון שלכם על מנת לעזור לכם למצוא מרחב מוגן קרוב ביותר גם במקומות ללא קליטה.',
              textDirection: TextDirection.rtl,
              textAlign: TextAlign.center,
            ),
            actionsAlignment: MainAxisAlignment.center,
            actions: [
              TextButton(
                onPressed: () => Navigator.of(ctx).pop(),
                child: const Text('הבנתי'),
              ),
            ],
          ),
    );
  } else if (wantsLastLocation == false) {
    await showDialog(
      context: context,
      barrierDismissible: false,
      builder:
          (ctx) => AlertDialog(
            title: const Text('שים לב', textAlign: TextAlign.center),
            content: const Text(
              'בעת שיבושי GPS האפליקציה לא תספק לך הגעה למרחב מוגן קרוב על בסיס מיקום אחרון.',
              textDirection: TextDirection.rtl,
              textAlign: TextAlign.center,
            ),
            actionsAlignment: MainAxisAlignment.center,
            actions: [
              TextButton(
                onPressed: () {
                  Navigator.of(ctx).pop();
                  Navigator.of(context).pushAndRemoveUntil(
                    MaterialPageRoute(builder: (_) => const EnterPage()),
                    (route) => false,
                  );
                },
                child: const Text('הבנתי'),
              ),
            ],
          ),
    );
  }
}
Future<bool> isLocationServiceEnabled() async {
  return await Geolocator.isLocationServiceEnabled();
}
// פונקציה לבדיקה ואזהרה על חיסכון סוללה
Future<void> checkBatteryOptimization(BuildContext context) async {
  final prefs = await SharedPreferences.getInstance();
  if (prefs.getBool('batteryOptimizationWarningShown') == true) {
    return;
  }
  await showDialog(
    context: context,
    barrierDismissible: false,
    builder:
        (ctx) => AlertDialog(
          title: const Text('הגדרות חיסכון סוללה', textAlign: TextAlign.center),
          content: const Text(
            'חשוב מאוד! כדי שהאפליקציה תוכל לשלוח מיקום גם כשהיא סגורה, יש להחריג אותה מחיסכון סוללה.\n\nצעדים:\n1. לחץ על "הגדרות"\n2. חפש "סוללה" או "Battery"\n3. בחר "חיסכון סוללה" או "Battery Optimization"\n4. מצא את האפליקציה ובחר "לא לבצע אופטימיזציה"\n\nזה הכרחי לקבלת התראות חירום!',
            textDirection: TextDirection.rtl,
            textAlign: TextAlign.center,
          ),
          actionsAlignment: MainAxisAlignment.center,
          actions: [
            TextButton(
              onPressed: () async {
                await prefs.setBool('batteryOptimizationWarningShown', true);
                Navigator.of(ctx).pop();
              },
              child: const Text('הבנתי'),
            ),
            TextButton(
              onPressed: () async {
                Navigator.of(ctx).pop();
                // פתיחת הגדרות המערכת
                await Geolocator.openAppSettings();
              },
              child: const Text('פתח הגדרות'),
            ),
          ],
        ),
  );
}
// Google Maps Renderer Initialization
Completer<AndroidMapRenderer?>? _initializedRendererCompleter;
/// Initializes map renderer to the `latest` renderer type.
/// The renderer must be requested before creating GoogleMap instances,
/// as the renderer can be initialized only once per application context.
Future<AndroidMapRenderer?> initializeMapRenderer() async {
  if (_initializedRendererCompleter != null) {
    return _initializedRendererCompleter!.future;
  }
  final Completer<AndroidMapRenderer?> completer =
      Completer<AndroidMapRenderer?>();
  _initializedRendererCompleter = completer;
  try {
    log('[MAPS] Initializing Google Maps renderer...');
    final GoogleMapsFlutterPlatform platform =
        GoogleMapsFlutterPlatform.instance;
    final AndroidMapRenderer initializedRenderer = await (platform
            as GoogleMapsFlutterAndroid)
        .initializeWithRenderer(AndroidMapRenderer.latest);
    log(
      '[MAPS] Google Maps renderer initialized successfully: $initializedRenderer',
    );
    completer.complete(initializedRenderer);
  } catch (e) {
    log('[MAPS] Failed to initialize Google Maps renderer: $e');
    completer.complete(null);
  }
  return completer.future;
}
void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Initialize Google Maps renderer first
  log('[INIT] Initializing Google Maps...');
  await initializeMapRenderer();
  log('[INIT] Google Maps initialization completed');
  await Firebase.initializeApp();
  // Initialize FCM services for background handling
  await initializeFCM();
  // Initialize Background Location Tracker with periodic updates
  await bg_tracker.BackgroundLocationTrackerManager.initialize(
    backgroundCallback,
    config: const bg_tracker.BackgroundLocationTrackerConfig(
      loggingEnabled: true,
      androidConfig: bg_tracker.AndroidConfig(
        notificationIcon: 'explore',
        trackingInterval: Duration(
          minutes: 15,
        ), // עדכון כל 15 דקות לשמירת סוללה
        distanceFilterMeters: 100, // עדכון רק אם זז 100 מטר
      ),
      iOSConfig: bg_tracker.IOSConfig(
        activityType: bg_tracker.ActivityType.FITNESS,
        distanceFilterMeters: 100,
        restartAfterKill: true,
      ),
    ),
  );
  // Start location tracking immediately for emergency readiness
  try {
    await bg_tracker.BackgroundLocationTrackerManager.startTracking();
    log('[INIT] Background location tracking started for emergency readiness');
  } catch (e) {
    log('[INIT] Failed to start background location tracking: $e');
  }
  // השתמש ב-handler מהקובץ firebase_messaging_service.dart בלבד
  FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
  final userData = await LocalStorageService.getUserData();
  final bool isLoggedIn =
      userData['user_id'] != null && userData['user_id'] != '';
  runApp(MyApp(isLoggedIn: isLoggedIn, userData: userData));
}
Timer? _globalLocationCheckTimer;
void startGlobalLocationServiceCheck(BuildContext context) {
  _globalLocationCheckTimer?.cancel();
  _globalLocationCheckTimer = Timer.periodic(const Duration(minutes: 5), (
    timer,
  ) async {
    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled && context.mounted) {
      // Optionally handle the case when location service is disabled
    }
  });
}
class MyApp extends StatelessWidget {
  final bool isLoggedIn;
  final Map<String, dynamic> userData;
  const MyApp({super.key, required this.isLoggedIn, required this.userData});
  @override
  Widget build(BuildContext context) {
    FirebaseMessaging.onMessage.listen((RemoteMessage message) {
      log(
        '[FCM] התקבלה הודעה בקדמה: ' +
            message.data.toString() +
            ' | notification: ' +
            (message.notification?.title ?? '') +
            ' - ' +
            (message.notification?.body ?? ''),
      );
      if (message.notification?.title == "התקבלה אזעקה") {
        // השתמש בפונקציה מ-firebase_messaging_service.dart
        sendLocationToServerFromFCM();
      }
    });
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'FC Flutter',
      // Hebrew localization configuration
      locale: const Locale('he', 'IL'), // Hebrew - Israel
      supportedLocales: const [
        Locale('he', 'IL'), // Hebrew - Israel
        Locale('en', 'US'), // English - United States (fallback)
      ],
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      theme: ThemeData(primarySwatch: Colors.blue),
      home: Builder(
        builder: (context) {
          ensureLocationPermissionAndService(context).then((_) {
            checkBatteryOptimization(context);
            startGlobalLocationServiceCheck(context);
          });
          if (isLoggedIn) {
            return MyHomePage(title: 'מסך הבית', userData: userData);
          } else {
            return const EnterPage();
          }
        },
      ),
      navigatorObservers: [routeObserver],
    );
  }
}
Future<void> ensureLocationPermissionAndServiceGlobal() async {
  bool permissionGranted = false;
  bool serviceEnabled = false;
  while (!permissionGranted || !serviceEnabled) {
    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      permission = await Geolocator.requestPermission();
    }
    permissionGranted =
        permission == LocationPermission.always ||
        permission == LocationPermission.whileInUse;
    serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!permissionGranted || !serviceEnabled) {
      // פתח את הגדרות המיקום
      await Geolocator.openLocationSettings();
      // המתן 2 שניות כדי לאפשר למשתמש להפעיל
      await Future.delayed(const Duration(seconds: 2));
    }
  }
}
//#end of file
*/