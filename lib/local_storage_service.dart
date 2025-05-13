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
    };

    // הדפסה ל-console
    print('נתוני המשתמש שנשלפו מה-Local Storage: $userData');
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

  Future<void> loadUserData(BuildContext context) async {
    try {
      final userData = await LocalStorageService.getUserData();

      print(
        'Navigating to PersonalDetailsPage with data: $userData',
      ); // Debugging
      Navigator.push(
        context,
        MaterialPageRoute(
          builder:
              (context) => PersonalDetailsPage(
                username: userData['Username'] ?? '',
                email: userData['Email'] ?? '',
                phoneNumber: userData['PhoneNumber'] ?? '',
              ),
        ),
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
    final url = Uri.parse('https://localhost:7203/api/User/UpdateUser');

    print(
      'UserId being sent to the server: ${userData['user_id']}',
    ); // Debugging UserId

    final updatedUser = {
      'UserId': userData['user_id'], // שימוש ב-ID של המשתמש המחובר
      'Username':
          usernameController.text.isNotEmpty
              ? usernameController.text
              : userData['username'],
      'PasswordHash':
          passwordController.text.isNotEmpty
              ? passwordController.text
              : userData['passwordHash'],
      'Email':
          emailController.text.isNotEmpty
              ? emailController.text
              : userData['email'],
      'PhoneNumber':
          phoneController.text.isNotEmpty
              ? phoneController.text
              : userData['phone_number'],
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

        // הדפסה ל-console
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
    final url = Uri.parse('https://localhost:7203/api/User/Login');
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
