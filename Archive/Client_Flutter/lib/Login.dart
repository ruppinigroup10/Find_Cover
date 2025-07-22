import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import 'home_page.dart'; // ייבוא דף הבית
import 'register.dart'; // ייבוא עמוד ההרשמה
import 'base_before_login.dart'; // ייבוא Base_before_login
import 'Enter.dart'; // ייבוא עמוד ההתחלה

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPage();
}

class _LoginPage extends State<LoginPage> {
  // Controllers לשדות הטקסט
  final TextEditingController emailController = TextEditingController();
  final TextEditingController passwordController = TextEditingController();

  bool _obscurePassword = true;

  // פונקציה לשליחת הנתונים לשרת (Login)
  Future<void> loginUser() async {
    final url = Uri.parse(
      'https://localhost:7203/api/User/Login',
    ); // כתובת ה-API
    try {
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'email': emailController.text,
          'passwordHash': passwordController.text,
        }),
      );

      if (!mounted) return; // Check if the widget is still mounted

      print('Response status: ${response.statusCode}');
      print('Response body: ${response.body}');

      if (response.statusCode == 200) {
        final responseData = jsonDecode(response.body);
        final user = responseData['user'];

        if (user == null) {
          print('User data is null');
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('שגיאה בטעינת נתוני המשתמש.'),
              backgroundColor: Colors.red,
            ),
          );
          return;
        }

        print('User data: $user');

        // שמירת נתוני המשתמש ב-Local Storage
        await saveUserData({
          'user_id': user['user_id']?.toString() ?? '',
          'username': user['username'] ?? '',
          'email': user['email'] ?? '',
          'phone_number': user['phone_number'] ?? '',
          'is_active': user['is_active']?.toString() ?? 'false',
          'is_provider': user['is_provider']?.toString() ?? 'false',
        });

        // מעבר לדף הבית
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (context) => MyHomePage(title: 'מסך הבית', userData: user),
          ),
        );
      } else {
        // שגיאה - הצגת הודעת שגיאה מותאמת
        final responseData = jsonDecode(response.body);
        print('Error response: $responseData');
        String errorMessage =
            responseData['message'] ?? 'שגיאה בהתחברות. נסה שוב.';
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(errorMessage), backgroundColor: Colors.red),
        );
      }
    } catch (e) {
      print('Exception: $e');
      if (!mounted) return; // Check if the widget is still mounted
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('שגיאה בחיבור לשרת.'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  Future<void> saveUserData(Map<String, dynamic> user) async {
    final prefs = await SharedPreferences.getInstance();
    // שמירת נתוני המשתמש עם התאמה לשמות השדות בשרת
    await prefs.setString('user_id', user['user_id'] ?? '');
    await prefs.setString('username', user['username'] ?? '');
    await prefs.setString('email', user['email'] ?? '');
    await prefs.setString('phone_number', user['phone_number'] ?? '');
    await prefs.setBool('is_active', user['is_active'] == 'true');
    await prefs.setBool('is_provider', user['is_provider'] == 'true');
  }

  Future<Map<String, String>> getUserData() async {
    final prefs = await SharedPreferences.getInstance();
    return {
      'user_id': prefs.getString('user_id') ?? '',
      'username': prefs.getString('username') ?? '',
      'email': prefs.getString('email') ?? '',
      'phone_number': prefs.getString('phone_number') ?? '',
    };
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 1.0),
          child: Column(
            children: [
              const SizedBox(height: 100),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_ios),
                    onPressed: () {
                      Navigator.pushReplacement(
                        context,
                        MaterialPageRoute(
                          builder: (context) => const EnterPage(),
                        ),
                      );
                    },
                  ),
                  Image.asset(
                    'assets/images/LOGO1.png', // עדכן את הנתיב לתמונה שלך
                    width: 100,
                    height: 100,
                  ),
                  const SizedBox(width: 48), // ריווח כדי לאזן את הלוגו
                ],
              ),
              const SizedBox(height: 10),
              const Text(
                'התחברות',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 24),
              ),
              const SizedBox(height: 30),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 30.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('כתובת מייל', style: TextStyle(fontSize: 16)),
                    const SizedBox(height: 5),
                    TextField(
                      controller: emailController,
                      textDirection: TextDirection.rtl, // כיוון טקסט RTL
                      decoration: InputDecoration(
                        hintText: 'הכנס כתובת מייל',
                        hintTextDirection: TextDirection.rtl, // כיוון טקסט ברמז
                        filled: true,
                        fillColor: const Color(0xFFB0C4DE),
                        contentPadding: const EdgeInsets.symmetric(
                          vertical: 10, // גובה קטן יותר
                          horizontal: 15, // ריווח פנימי לרוחב
                        ),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(10),
                          borderSide: BorderSide.none,
                        ),
                      ),
                    ),
                    const SizedBox(height: 20),
                    const Text('סיסמה', style: TextStyle(fontSize: 16)),
                    const SizedBox(height: 5),
                    TextField(
                      controller: passwordController,
                      obscureText: _obscurePassword,
                      textDirection: TextDirection.rtl, // כיוון טקסט RTL
                      decoration: InputDecoration(
                        hintText: 'הכנס סיסמה',
                        hintTextDirection: TextDirection.rtl, // כיוון טקסט ברמז
                        filled: true,
                        fillColor: const Color(0xFFB0C4DE),
                        contentPadding: const EdgeInsets.symmetric(
                          vertical: 10, // גובה קטן יותר
                          horizontal: 15, // ריווח פנימי לרוחב
                        ),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(10),
                          borderSide: BorderSide.none,
                        ),
                        suffixIcon: Checkbox(
                          value: !_obscurePassword,
                          onChanged: (bool? value) {
                            setState(() {
                              _obscurePassword = !(value ?? false);
                            });
                          },
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 30),
              ElevatedButton(
                onPressed: loginUser, // קריאה לפונקציית ההתחברות
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 40,
                    vertical: 15,
                  ),
                ),
                child: const Text('כניסה', style: TextStyle(fontSize: 16)),
              ),
              const SizedBox(height: 10),
              GestureDetector(
                onTap: () {
                  // מעבר לעמוד ההרשמה
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const RegisterPage(),
                    ),
                  );
                },
                child: const Text(
                  'אין לך משתמש עדיין? הרשם עכשיו!',
                  style: TextStyle(color: Colors.blue),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
