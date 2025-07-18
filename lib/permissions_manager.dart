import 'package:flutter/material.dart';
import 'package:permission_handler/permission_handler.dart';

class PermissionsManager {
  /// בודק ומבקש את כל ההרשאות הנדרשות לאפליקציה
  static Future<bool> requestAllPermissions(BuildContext context) async {
    bool allGranted = true;

    // בקשת הרשאת מיקום (תמיד)
    bool locationGranted = await _requestLocationPermissions(context);
    if (!locationGranted) allGranted = false;

    // בקשת הרשאת התראות
    bool notificationGranted = await _requestNotificationPermission(context);
    if (!notificationGranted) allGranted = false;

    // בקשת הרשאת סוללה (לא מוגבל)
    bool batteryGranted = await _requestBatteryOptimizationPermission(context);
    if (!batteryGranted) allGranted = false;

    return allGranted;
  }

  /// בקשת הרשאות מיקום
  static Future<bool> _requestLocationPermissions(BuildContext context) async {
    // בודק הרשאת מיקום בסיסית
    var locationStatus = await Permission.location.status;
    if (locationStatus.isDenied) {
      locationStatus = await Permission.location.request();
    }

    // אם ההרשאה בסיסית ניתנה, מבקש הרשאת מיקום תמיד
    if (locationStatus.isGranted) {
      var locationAlwaysStatus = await Permission.locationAlways.status;
      if (locationAlwaysStatus.isDenied) {
        // מציג הסבר למשתמש
        await _showLocationAlwaysDialog(context);
        locationAlwaysStatus = await Permission.locationAlways.request();
      }

      if (locationAlwaysStatus.isPermanentlyDenied) {
        await _showPermissionSettingsDialog(
          context,
          'הרשאת מיקום תמיד',
          'כדי שהאפליקציה תוכל לעבוד ברקע, יש צורך בהרשאת מיקום "תמיד".\nלחץ על "פתח הגדרות" ובחר "מיקום" -> "כן, כל הזמן"',
        );
        return false;
      }

      return locationAlwaysStatus.isGranted;
    }

    return false;
  }

  /// בקשת הרשאת התראות
  static Future<bool> _requestNotificationPermission(
    BuildContext context,
  ) async {
    var notificationStatus = await Permission.notification.status;

    if (notificationStatus.isDenied) {
      // מציג הסבר למשתמש
      await _showNotificationDialog(context);
      notificationStatus = await Permission.notification.request();
    }

    if (notificationStatus.isPermanentlyDenied) {
      await _showPermissionSettingsDialog(
        context,
        'הרשאת התראות',
        'כדי לקבל התראות חירום, יש צורך להפעיל התראות.\nלחץ על "פתח הגדרות" ובחר "התראות" -> "הפעל"',
      );
      return false;
    }

    return notificationStatus.isGranted;
  }

  /// בקשת הרשאת סוללה (לא מוגבל)
  static Future<bool> _requestBatteryOptimizationPermission(
    BuildContext context,
  ) async {
    var batteryStatus = await Permission.ignoreBatteryOptimizations.status;

    if (batteryStatus.isDenied) {
      // מציג הסבר למשתמש
      await _showBatteryOptimizationDialog(context);
      batteryStatus = await Permission.ignoreBatteryOptimizations.request();
    }

    if (batteryStatus.isPermanentlyDenied) {
      await _showPermissionSettingsDialog(
        context,
        'אופטימיזציית סוללה',
        'כדי שהאפליקציה תוכל לעבוד ברקע ללא הפרעה, יש לבטל את אופטימיזציית הסוללה.\nלחץ על "פתח הגדרות" ובחר "סוללה" -> "חיסכון סוללה" -> החרג את האפליקציה',
      );
      return false;
    }

    return batteryStatus.isGranted;
  }

  /// דיאלוג הסבר להרשאת מיקום תמיד
  static Future<void> _showLocationAlwaysDialog(BuildContext context) async {
    return showDialog(
      context: context,
      barrierDismissible: false,
      builder:
          (context) => AlertDialog(
            title: const Text(
              'הרשאת מיקום תמיד',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            content: const Text(
              'כדי שהאפליקציה תוכל לשלוח את מיקומך במקרה חירום גם כשהיא לא פתוחה, יש צורך בהרשאת מיקום "תמיד".\n\nזה חיוני לתפקוד האפליקציה במצבי חירום.',
              textAlign: TextAlign.center,
              textDirection: TextDirection.rtl,
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('הבנתי'),
              ),
            ],
          ),
    );
  }

  /// דיאלוג הסבר להרשאת התראות
  static Future<void> _showNotificationDialog(BuildContext context) async {
    return showDialog(
      context: context,
      barrierDismissible: false,
      builder:
          (context) => AlertDialog(
            title: const Text(
              'הרשאת התראות',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            content: const Text(
              'כדי לקבל התראות חירום חשובות, יש צורך להפעיל התראות.\n\nזה יאפשר לך לקבל עדכונים על מצבי חירום באזורך.',
              textAlign: TextAlign.center,
              textDirection: TextDirection.rtl,
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('הבנתי'),
              ),
            ],
          ),
    );
  }

  /// דיאלוג הסבר לאופטימיזציית סוללה
  static Future<void> _showBatteryOptimizationDialog(
    BuildContext context,
  ) async {
    return showDialog(
      context: context,
      barrierDismissible: false,
      builder:
          (context) => AlertDialog(
            title: const Text(
              'אופטימיזציית סוללה',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            content: const Text(
              'כדי שהאפליקציה תוכל לעבוד ברקע ללא הפרעות, יש לבטל את אופטימיזציית הסוללה.\n\nזה יבטיח שהאפליקציה תוכל לשלוח מיקום במצבי חירום.',
              textAlign: TextAlign.center,
              textDirection: TextDirection.rtl,
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('הבנתי'),
              ),
            ],
          ),
    );
  }

  /// דיאלוג כללי להפניה להגדרות
  static Future<void> _showPermissionSettingsDialog(
    BuildContext context,
    String permissionName,
    String message,
  ) async {
    return showDialog(
      context: context,
      barrierDismissible: false,
      builder:
          (context) => AlertDialog(
            title: Text(
              permissionName,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            content: Text(
              message,
              textAlign: TextAlign.center,
              textDirection: TextDirection.rtl,
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('ביטול'),
              ),
              TextButton(
                onPressed: () {
                  Navigator.of(context).pop();
                  openAppSettings();
                },
                child: const Text('פתח הגדרות'),
              ),
            ],
          ),
    );
  }

  /// בודק שכל הדרישות עבור Google Maps מתקיימות
  static Future<Map<String, bool>> checkGoogleMapsRequirements() async {
    Map<String, bool> requirements = {
      'internet': false,
      'location': false,
      'locationAlways': false,
    };

    try {
      // בדיקת הרשאת מיקום
      var locationStatus = await Permission.location.status;
      requirements['location'] = locationStatus.isGranted;

      // בדיקת הרשאת מיקום תמיד
      var locationAlwaysStatus = await Permission.locationAlways.status;
      requirements['locationAlways'] = locationAlwaysStatus.isGranted;

      // סימולציה לבדיקת אינטרנט (מקרוב)
      requirements['internet'] = true;

      print("PermissionsManager: Google Maps requirements check:");
      print("  - Internet: ${requirements['internet']}");
      print("  - Location: ${requirements['location']}");
      print("  - Location Always: ${requirements['locationAlways']}");
    } catch (e) {
      print("PermissionsManager: Error checking Google Maps requirements: $e");
    }

    return requirements;
  }

  /// מציג דיאלוג עם פרטי הדרישות החסרות עבור Google Maps
  static Future<void> showGoogleMapsRequirementsDialog(
    BuildContext context,
    Map<String, bool> requirements,
  ) async {
    List<String> missingRequirements = [];

    if (!requirements['internet']!) {
      missingRequirements.add('חיבור אינטרנט');
    }
    if (!requirements['location']!) {
      missingRequirements.add('הרשאת מיקום');
    }
    if (!requirements['locationAlways']!) {
      missingRequirements.add('הרשאת מיקום תמיד');
    }

    if (missingRequirements.isEmpty) return;

    await showDialog(
      context: context,
      barrierDismissible: false,
      builder:
          (context) => AlertDialog(
            title: const Text(
              'דרישות חסרות עבור מפת Google',
              textAlign: TextAlign.center,
            ),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.map_outlined, size: 48, color: Colors.orange),
                const SizedBox(height: 16),
                const Text(
                  'כדי שהמפה תוצג כראוי, נדרשות ההרשאות הבאות:',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                ...missingRequirements.map(
                  (req) => Text(
                    '• $req',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                    textAlign: TextAlign.center,
                  ),
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('אישור'),
              ),
              if (requirements['location'] == false ||
                  requirements['locationAlways'] == false)
                TextButton(
                  onPressed: () async {
                    Navigator.of(context).pop();
                    await openAppSettings();
                  },
                  child: const Text('פתח הגדרות'),
                ),
            ],
          ),
    );
  }
}
