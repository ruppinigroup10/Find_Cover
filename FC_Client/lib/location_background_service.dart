// קובץ זה מכיל את הלוגיקה לשליחת מיקום לשרת כל 15 דקות, גם ברקע וגם כשהאפליקציה פתוחה
import 'dart:async';
import 'dart:io';
import 'dart:convert';
import 'package:geolocator/geolocator.dart';
import 'package:geolocator/geolocator.dart' as geo;
import 'package:workmanager/workmanager.dart';
import 'package:http/http.dart' as http;
import 'package:flutter/foundation.dart';
import 'local_storage_service.dart'; // ייבוא שירות לשליפת userId

// שם המשימה של Workmanager
const String locationTaskName = 'sendLocationTask';

// הפונקציה שנקראת ע"י Workmanager ברקע
@pragma('vm:entry-point')
void callbackDispatcher() {
  Workmanager().executeTask((task, inputData) async {
    try {
      // שליפת userId מה-local storage
      final userData = await LocalStorageService.getUserData();
      final userId = userData['user_id'] ?? '';
      if (userId == '' || userId == null) {
        if (kDebugMode) print('userId לא נמצא, לא נשלח מיקום');
        return Future.value(true);
      }
      Position position = await Geolocator.getCurrentPosition(
        desiredAccuracy: geo.LocationAccuracy.high,
      );
      final url =
          'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Location/AddUserLocation';
      final body = {
        'userId': int.tryParse(userId) ?? 0,
        'latitude': position.latitude,
        'longitude': position.longitude,
        'createdAt':
            DateTime.now()
                .toUtc()
                .add(const Duration(hours: 3))
                .toIso8601String(),
      };
      if (kDebugMode) print('שולח מיקום ברקע: $body ל-$url');
      await http.post(
        Uri.parse(url),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(body),
      );
    } catch (e) {
      if (kDebugMode) print('שגיאה בשליחת מיקום: $e');
    }
    return Future.value(true);
  });
}

// פונקציה שמפעילה שליחת מיקום כל 15 דקות ברקע
void initializeBackgroundLocation() {
  if (Platform.isAndroid || Platform.isIOS) {
    // הפעלת Workmanager גם באנדרואיד וגם ב-iOS
    Workmanager().initialize(callbackDispatcher, isInDebugMode: false);
    // הרשמה למשימה חוזרת כל 15 דקות (או 3 דקות אם אפשר)
    Workmanager().registerPeriodicTask(
      locationTaskName,
      locationTaskName,
      frequency: const Duration(minutes: 3), // נסה כל 3 דקות
      initialDelay: const Duration(seconds: 10),
      existingWorkPolicy: ExistingWorkPolicy.replace,
      constraints: Constraints(networkType: NetworkType.connected),
    );
    if (kDebugMode)
      print('Workmanager periodic task registered for every 3 minutes');
  } else {
    print('Workmanager not supported on this platform');
  }
}

// פונקציה לשליחת מיקום כאשר האפליקציה פתוחה (כל 15 דקות)
Timer? _foregroundTimer;
void startForegroundLocationUpdates() async {
  // בקשת הרשאות מיקום (כולל בקשה להפעיל שירותי מיקום אם כבוי)
  LocationPermission permission = await Geolocator.checkPermission();
  if (permission == LocationPermission.denied ||
      permission == LocationPermission.deniedForever) {
    permission = await Geolocator.requestPermission();
  }
  // בדיקה אם שירותי מיקום דלוקים, אם לא - בקש מהמשתמש להפעיל
  bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
  if (!serviceEnabled) {
    await Geolocator.openLocationSettings();
  }
  // שליפת userId מה-local storage
  final userData = await LocalStorageService.getUserData();
  final userId = userData['user_id'] ?? '';
  if (userId == '' || userId == null) {
    if (kDebugMode) print('userId לא נמצא, לא נשלח מיקום');
    return;
  }
  _foregroundTimer?.cancel();
  // הפעל טיימר קבוע כל 3 דקות (ללא שליחה מיידית)
  _foregroundTimer = Timer.periodic(const Duration(minutes: 3), (timer) async {
    try {
      Position position = await Geolocator.getCurrentPosition(
desiredAccuracy: geo.LocationAccuracy.high,
      );
      final url =
          'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Location/AddUserLocation';
      final body = {
        'userId': int.tryParse(userId) ?? 0,
        'latitude': position.latitude,
        'longitude': position.longitude,
        'createdAt':
            DateTime.now()
                .toUtc()
                .add(const Duration(hours: 3))
                .toIso8601String(),
      };
      if (kDebugMode) print('שולח מיקום (foreground): $body ל-$url');
      await http.post(
        Uri.parse(url),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(body),
      );
    } catch (e) {
      if (kDebugMode) print('שגיאה בשליחת מיקום (foreground): $e');
    }
  });
}

void stopForegroundLocationUpdates() {
  _foregroundTimer?.cancel();
}

Future<void> sendLocationNow() async {
  try {
    final userData = await LocalStorageService.getUserData();
    final userId = userData['user_id'] ?? '';
    if (userId == '' || userId == null) {
      if (kDebugMode) print('userId לא נמצא, לא נשלח מיקום');
      return;
    }
    Position position = await Geolocator.getCurrentPosition(
desiredAccuracy: geo.LocationAccuracy.high,
    );
    final url =
        'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Location/AddUserLocation';
    final body = {
      'userId': int.tryParse(userId) ?? 0,
      'latitude': position.latitude,
      'longitude': position.longitude,
      'createdAt':
          DateTime.now()
              .toUtc()
              .add(const Duration(hours: 3))
              .toIso8601String(),
    };
    if (kDebugMode) print('שולח מיקום מיידי: $body ל-$url');
    await http.post(
      Uri.parse(url),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(body),
    );
  } catch (e) {
    if (kDebugMode) print('שגיאה בשליחת מיקום מיידי: $e');
  }
}

Future<Map<String, dynamic>> sendLocationNowWithCreatedAt(
  String createdAt,
) async {
  Map<String, dynamic> body = {};
  try {
    final userData = await LocalStorageService.getUserData();
    final userId = userData['user_id'] ?? '';
    if (userId == '' || userId == null) {
      if (kDebugMode) print('userId לא נמצא, לא נשלח מיקום');
      return {};
    }
    Position position = await Geolocator.getCurrentPosition(
desiredAccuracy: geo.LocationAccuracy.high,
    );
    final url =
        'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Location/AddUserLocation';
    body = {
      'userId': int.tryParse(userId) ?? 0,
      'latitude': position.latitude,
      'longitude': position.longitude,
      'createdAt': createdAt,
    };
    if (kDebugMode) print('שולח מיקום מיידי: $body ל-$url');
    final response = await http.post(
      Uri.parse(url),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(body),
    );
    if (kDebugMode)
      print('תשובת שרת (immediate): ${response.statusCode} ${response.body}');
    return body;
  } catch (e) {
    if (kDebugMode) print('שגיאה בשליחת מיקום מיידי: $e');
    return body;
  }
}
