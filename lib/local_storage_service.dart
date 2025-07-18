// saveUserData - שומר נתוני משתמש חדשים ומנסה לשלוף shelter_id מהשרת לפי shelter_id (אם קיים)
// saveUserDataToPreferences - שומר נתוני משתמש ב-SharedPreferences (שמות שדות עם אות גדולה)
// getUserData - שולף נתוני משתמש מה-SharedPreferences (שמות שדות עם אות קטנה)
// getUserDataFromPreferences - שולף נתוני משתמש מה-SharedPreferences (שמות שדות עם אות גדולה)
// saveLoggedInUser - שומר נתוני משתמש מחובר, ושולח בקשת GET לשרת:
//   -> /api/Shelter/getMyShelter?provider_id=... (מחזיר shelter לפי provider_id)
//   אם קיים, שומר את כל נתוני המקלט ב-SharedPreferences
// getLoggedInUser - שולף נתוני משתמש מחובר מה-SharedPreferences
// updateUserData - שולח בקשת PUT לשרת:
//   -> /api/User/UpdateUser (לעדכון נתוני משתמש)
// loginUser - שולח בקשת POST לשרת:
//   -> /api/User/Login (להתחברות משתמש)
//   אם הצליח, שומר נתוני משתמש בקובץ

import 'package:shared_preferences/shared_preferences.dart';
import 'package:flutter/material.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'personal_details.dart'; // Importing PersonalDetailsPage

class LocalStorageService {
  final TextEditingController phoneController = TextEditingController();
  final TextEditingController emailController = TextEditingController();
  final TextEditingController usernameController = TextEditingController();
  final TextEditingController passwordController = TextEditingController();

  static Future<void> saveUserData(Map<String, dynamic> user) async {
    final prefs = await SharedPreferences.getInstance();
    print('PasswordHash received: ${user['PasswordHash']}'); // Debugging
    await prefs.setString('user_id', user['user_id'].toString());
    await prefs.setString('username', user['username']);
    await prefs.setString('email', user['email']);
    await prefs.setString('phone_number', user['phone_number']);
    await prefs.setBool('is_active', user['is_active']);
    await prefs.setBool('is_provider', user['is_provider']);
    // שמירת הסיסמה (PasswordHash) אם קיימת
    if (user['PasswordHash'] != null &&
        user['PasswordHash'].toString().isNotEmpty) {
      await prefs.setString('PasswordHash', user['PasswordHash']);
    }
    // שליפת shelter_id מהשרת (אם קיים)
    if (user['shelter_id'] != null &&
        user['shelter_id'].toString().isNotEmpty) {
      try {
        final providerId = user['UserId'] ?? user['user_id'];
        if (providerId != null && providerId.toString().isNotEmpty) {
          final response = await http.get(
            Uri.parse(
              'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/getMyShelter?provider_id=$providerId',
            ),
          );
          if (response.statusCode == 200) {
            final decoded = jsonDecode(response.body);
            if (decoded['shlter'] != null) {
              await prefs.setString(
                'shelter_id',
                decoded['shlter']['shelter_id'].toString(),
              );
              print(
                'shelter_id נשמר בהצלחה: ${decoded['shlter']['shelter_id']}',
              );
            }
          }
        }
      } catch (e) {
        print('שגיאה בשליפת shelter_id: $e');
      }
    }
    // הדפסה ל-console
    print('נתוני המשתמש נשמרו בהצלחה: $user');
  }

  static Future<Map<String, dynamic>> getUserData() async {
    final prefs = await SharedPreferences.getInstance();
    final userData = {
      'user_id': prefs.getString('user_id') ?? '',
      'username': prefs.getString('username') ?? '',
      'email': prefs.getString('email') ?? '',
      'phone_number': prefs.getString('phone_number') ?? '',
      'is_active': prefs.getBool('is_active') ?? false,
      'is_provider':
          prefs.getBool('is_provider') ??
          false, // Fixed key to match correct casing
      'passwordHash':
          prefs.getString('PasswordHash') ?? '', // הוספת שליפת סיסמה
    };

    // הדפסה ל-console
    print('נתוני המשתמש שנשלפו מה-Local Storage: $userData');
    print(
      'shelter_id שנשלף מה-SharedPreferences: [33m${prefs.getString('shelter_id')}[0m',
    );
    return userData;
  }

  static Future<void> clearUserData() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.clear();
  }

  static Future<void> saveUserDataToPreferences(
    Map<String, dynamic> user,
  ) async {
    final prefs = await SharedPreferences.getInstance();
    print('PasswordHash received: ${user['PasswordHash']}'); // Debugging
    await prefs.setString('UserId', user['UserId'].toString());
    await prefs.setString('Username', user['Username']);
    await prefs.setString('Email', user['Email']);
    await prefs.setString('PhoneNumber', user['PhoneNumber']);

    // הדפסה ל-console
    print('נתוני המשתמש נשמרו בהצלחה ב-SharedPreferences: $user');
  }

  static Future<Map<String, dynamic>> getUserDataFromPreferences() async {
    final prefs = await SharedPreferences.getInstance();
    final userData = {
      'UserId': prefs.getString('UserId') ?? '',
      'Username': prefs.getString('Username') ?? '',
      'Email': prefs.getString('Email') ?? '',
      'PhoneNumber': prefs.getString('PhoneNumber') ?? '',
      'CreatedAt': prefs.getString('CreatedAt') ?? '',
      'IsActive': prefs.getBool('IsActive') ?? false,
      'IsProvider': prefs.getBool('IsProvider') ?? false,
    };

    // הדפסה ל-console
    print('נתוני המשתמש שנשלפו מ-SharedPreferences: $userData');
    return userData;
  }

  static Future<void> saveLoggedInUser(Map<String, dynamic> user) async {
    final prefs = await SharedPreferences.getInstance();
    print('PasswordHash received: ${user['PasswordHash']}'); // Debugging
    await prefs.setString('UserId', user['UserId'].toString());
    await prefs.setString('Username', user['Username']);
    await prefs.setString('Email', user['Email']);
    await prefs.setString('PhoneNumber', user['PhoneNumber']);
    await prefs.setString('PasswordHash', user['PasswordHash']); // שמירת הסיסמה
    // שמירת shelter_id אם קיים
    if (user['shelter_id'] != null &&
        user['shelter_id'].toString().isNotEmpty) {
      await prefs.setString('shelter_id', user['shelter_id'].toString());
      print('shelter_id נשמר למשתמש המחובר: ${user['shelter_id']}');
    }
    // שליפת shelter מהשרת לפי provider_id (user_id)
    if (user['UserId'] != null && user['UserId'].toString().isNotEmpty) {
      try {
        final providerId = user['UserId'];
        print('provider_id הנשלח לשרת: $providerId'); // debug
        final response = await http.get(
          Uri.parse(
            'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/getMyShelter?provider_id=$providerId',
          ),
        );
        if (response.statusCode == 200) {
          final decoded = jsonDecode(response.body);
          if (decoded['shlter'] != null) {
            final shelter = decoded['shlter'];
            await prefs.setString(
              'shelter_id',
              shelter['shelter_id'].toString(),
            );
            print('shelter_id נשמר בהצלחה: ${shelter['shelter_id']}');
            await prefs.setString(
              'shelter_type',
              shelter['shelter_type']?.toString() ?? '',
            );
            await prefs.setString(
              'shelter_name',
              shelter['name']?.toString() ?? '',
            );
            await prefs.setString(
              'shelter_latitude',
              shelter['latitude']?.toString() ?? '',
            );
            await prefs.setString(
              'shelter_longitude',
              shelter['longitude']?.toString() ?? '',
            );
            await prefs.setString(
              'shelter_address',
              shelter['address']?.toString() ?? '',
            );
            await prefs.setString(
              'shelter_capacity',
              shelter['capacity']?.toString() ?? '',
            );
            await prefs.setBool(
              'shelter_is_accessible',
              shelter['is_accessible'] ?? false,
            );
            await prefs.setBool(
              'shelter_is_active',
              shelter['is_active'] ?? false,
            );
            await prefs.setString(
              'shelter_additional_information',
              shelter['additional_information']?.toString() ?? '',
            );
            await prefs.setString(
              'shelter_created_at',
              shelter['created_at']?.toString() ?? '',
            );
            await prefs.setString(
              'shelter_last_updated',
              shelter['last_updated']?.toString() ?? '',
            );
            print('פרטי המקלט נשמרו ב-SharedPreferences: $shelter');
          }
        }
      } catch (e) {
        print('שגיאה בשליפת shelter לפי provider_id: $e');
      }
    }
    // הדפסה ל-console
    print('משתמש מחובר נשמר בהצלחה ב-SharedPreferences: $user');
  }

  static Future<Map<String, dynamic>> getLoggedInUser() async {
    final prefs = await SharedPreferences.getInstance();
    final userData = {
      'UserId': prefs.getString('UserId') ?? '',
      'Username': prefs.getString('Username') ?? '',
      'Email': prefs.getString('Email') ?? '',
      'PhoneNumber': prefs.getString('PhoneNumber') ?? '',
      'CreatedAt': prefs.getString('CreatedAt') ?? '',
      'IsActive': prefs.getBool('IsActive') ?? false,
      'IsProvider': prefs.getBool('IsProvider') ?? false,
    };

    // הדפסה ל-console
    print('משתמש מחובר שנשלף מ-SharedPreferences: $userData');
    return userData;
  }

  // שליפת נתוני המקלט מהשרת לפי ה-UserId ושמירתם ב-SharedPreferences
  static Future<void> fetchAndSaveShelterForCurrentUser() async {
    print('fetchAndSaveShelterForCurrentUser: התחלה');
    final prefs = await SharedPreferences.getInstance();
    final keys = prefs.getKeys();
    for (var key in keys) {
      print('DEBUG: prefs[$key] = ${prefs.get(key)}');
    }
    final providerId = prefs.getString('UserId') ?? '';
    print('fetchAndSaveShelterForCurrentUser: providerId = $providerId');
    if (providerId.isNotEmpty) {
      final response = await http.get(
        Uri.parse(
          'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/getMyShelter?provider_id=$providerId',
        ),
      );
      print(
        'fetchAndSaveShelterForCurrentUser: response.body = ${response.body}',
      );
      if (response.statusCode == 200) {
        final decoded = jsonDecode(response.body);
        if (decoded['shlter'] != null) {
          final shelter = decoded['shlter'];
          await prefs.setString('shelter_id', shelter['shelter_id'].toString());
          await prefs.setString(
            'shelter_type',
            shelter['shelter_type']?.toString() ?? '',
          );
          await prefs.setString(
            'shelter_name',
            shelter['name']?.toString() ?? '',
          );
          await prefs.setString(
            'shelter_latitude',
            shelter['latitude']?.toString() ?? '',
          );
          await prefs.setString(
            'shelter_longitude',
            shelter['longitude']?.toString() ?? '',
          );
          await prefs.setString(
            'shelter_address',
            shelter['address']?.toString() ?? '',
          );
          await prefs.setString(
            'shelter_capacity',
            shelter['capacity']?.toString() ?? '',
          );
          await prefs.setBool(
            'shelter_is_accessible',
            shelter['is_accessible'] ?? false,
          );
          await prefs.setBool(
            'shelter_is_active',
            shelter['is_active'] ?? false,
          );
          await prefs.setString(
            'shelter_additional_information',
            shelter['additional_information']?.toString() ?? '',
          );
          await prefs.setString(
            'shelter_created_at',
            shelter['created_at']?.toString() ?? '',
          );
          await prefs.setString(
            'shelter_last_updated',
            shelter['last_updated']?.toString() ?? '',
          );
          print(
            'fetchAndSaveShelterForCurrentUser: shelter_id נשמר: ${shelter['shelter_id']}',
          );
        }
      }
    }
  }

  Future<void> loadUserData(BuildContext context) async {
    try {
      final userData = await LocalStorageService.getUserData();
      print(
        'Navigating to PersonalDetailsPage with data: $userData',
      ); // Debugging
      Navigator.push(
        context,
        MaterialPageRoute(builder: (context) => const PersonalDetailsPage()),
      );
    } catch (e) {
      print('Error loading user data: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Error loading user data: $e'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  Future<void> updateUserData(BuildContext context) async {
    final userData = await LocalStorageService.getLoggedInUser();
    final url = Uri.parse(
      'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/UpdateUser',
    );

    print(
      'UserId being sent to the server: ${userData['UserId']}',
    ); // Debugging UserId

    final updatedUser = {
      'UserId': userData['UserId'], // שימוש ב-ID של המשתמש המחובר
      'Username':
          usernameController.text.isNotEmpty
              ? usernameController.text
              : userData['Username'],
      'PasswordHash':
          passwordController.text.isNotEmpty
              ? passwordController.text
              : userData['PasswordHash'],
      'Email':
          emailController.text.isNotEmpty
              ? emailController.text
              : userData['Email'],
      'PhoneNumber':
          phoneController.text.isNotEmpty
              ? phoneController.text
              : userData['PhoneNumber'],
    };

    // הדפסה ל-console
    print('נתונים שנשלחים לשרת לעדכון: $updatedUser');

    try {
      final response = await http.put(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(updatedUser),
      );

      if (response.statusCode == 200) {
        await LocalStorageService.saveLoggedInUser(updatedUser);
        print(
          'נתוני המשתמש עודכנו בהצלחה בשרת וב-SharedPreferences: $updatedUser',
        );
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('העדכון הושלם בהצלחה!'),
            backgroundColor: Colors.green,
          ),
        );
      } else {
        print('שגיאה בעדכון הנתונים בשרת: ${response.body}');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('שגיאה בעדכון הנתונים בשרת: ${response.body}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } catch (e) {
      print('שגיאה בחיבור לשרת: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('שגיאה בחיבור לשרת: $e'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  Future<void> loginUser(BuildContext context) async {
    final url = Uri.parse(
      'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/Login',
    );
    try {
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'email': emailController.text,
          'passwordHash': passwordController.text,
        }),
      );

      if (response.statusCode == 200) {
        final responseData = jsonDecode(response.body);
        final user = responseData['user'];
        // השמה של user_id לשדה provider_id
        user['provider_id'] = user['user_id'];
        // שמירת נתוני המשתמש בקובץ
        await saveUserDataToFile(user);
        // הדפסת נתוני המשתמש ל-console
        print('משתמש התחבר בהצלחה: $user');
        // מעבר לדף הבית
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder:
                (context) =>
                    const Placeholder(), // Replace with actual HomePage implementation
          ),
        );
      } else {
        print('שגיאה בהתחברות: ${response.body}');
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('שגיאה בהתחברות.'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } catch (e) {
      print('שגיאה בחיבור לשרת: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('שגיאה בחיבור לשרת.'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  Future<void> saveUserDataToFile(Map<String, dynamic> user) async {
    // Removed duplicate declaration of saveUserDataToFile
  }
}
