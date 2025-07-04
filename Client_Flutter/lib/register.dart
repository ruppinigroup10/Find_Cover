import 'package:flutter/material.dart';
//import 'package:url_launcher/url_launcher.dart'; // ייבוא חבילת url_launcher
import 'package:http/http.dart' as http;
import 'dart:convert';
import 'base_before_login.dart'; // ייבוא Base_before_login
import 'Enter.dart'; // ייבוא עמוד ההתחלה

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  // Controllers לשדות הטקסט
  final TextEditingController nameController = TextEditingController();
  final TextEditingController emailController = TextEditingController();
  final TextEditingController phoneController = TextEditingController();
  final TextEditingController passwordController = TextEditingController();

  // מפתח לטופס
  final _formKey = GlobalKey<FormState>();

  // משתנה למצב הצגת סיסמה
  bool _obscurePassword = true;

  // פונקציה לשליחת הנתונים לשרת
  Future<void> registerUser() async {
    print('registerUser called');
    if (!_formKey.currentState!.validate()) {
      print('Form validation failed');
      return; // אם הטופס לא תקין, אל תמשיך
    }

    final url = Uri.parse(
      'https://localhost:7203/api/User/Register',
    ); // כתובת ה-API
    print('Sending registration request to: $url');
    print(
      'Payload: username=${nameController.text}, email=${emailController.text}, phoneNumber=${phoneController.text}, passwordHash=${passwordController.text}',
    );
    try {
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'username': nameController.text,
          'email': emailController.text,
          'phoneNumber': phoneController.text,
          'passwordHash': passwordController.text,
        }),
      );
      print('Response status: \\${response.statusCode}');
      print('Response body: \\${response.body}');
      if (response.statusCode == 200) {
        // הצלחה - מעבר לדף הבית
        print('Registration successful! Navigating to EnterPage.');
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('ההרשמה בוצעה בהצלחה!')));
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(builder: (context) => const EnterPage()),
        );
      } else {
        // שגיאה - הצגת הודעת שגיאה מותאמת
        print('Registration failed. Status: \\${response.statusCode}');
        final responseData = jsonDecode(response.body);
        String errorMessage = 'שגיאה בהרשמה. נסה שוב.';
        if (responseData['message'] != null) {
          errorMessage = responseData['message'];
        }
        print('Error message: $errorMessage');
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(errorMessage)));
      }
    } catch (e) {
      print('Error: $e');
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('שגיאה בחיבור לשרת.')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Center(
          child: SingleChildScrollView(
            child: Form(
              key: _formKey, // שימוש במפתח הטופס
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
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
                    'הרשמה',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 24),
                  ),
                  const SizedBox(height: 10),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 30.0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('* שם מלא', style: TextStyle(fontSize: 16)),
                        const SizedBox(height: 5),
                        TextFormField(
                          controller: nameController,
                          decoration: InputDecoration(
                            hintText: 'הכנס שם מלא',
                            filled: true,
                            fillColor: const Color(0xFFB0C4DE),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(10),
                              borderSide: BorderSide.none,
                            ),
                          ),
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'שם מלא נדרש';
                            }
                            // בדיקה שמכיל לפחות שתי מילים
                            final words = value.trim().split(RegExp(r'\s+'));
                            if (words.length < 2) {
                              return 'נא להכניס שם מלא';
                            }
                            // בדיקה שאין מספרים
                            if (RegExp(r'[0-9]').hasMatch(value)) {
                              return 'השם חייב להכיל אותיות בלבד';
                            }
                            // בדיקה לאותיות בלבד (עברית או אנגלית)
                            if (!RegExp(r'^[a-zA-Zא-ת\s]+$').hasMatch(value)) {
                              return 'השם חייב להכיל אותיות בלבד בעברית או באנגלית';
                            }
                            // אם באנגלית, כל מילה חייבת להתחיל באות גדולה
                            if (RegExp(r'^[A-Za-z ]+$').hasMatch(value)) {
                              for (final word in words) {
                                if (word.isNotEmpty &&
                                    word[0] != word[0].toUpperCase()) {
                                  return 'נא להתחיל באות גדולה';
                                }
                              }
                            }
                            if (value.length < 3) {
                              return 'שם מלא חייב להכיל לפחות 3 תווים';
                            }
                            return null;
                          },
                        ),
                        const SizedBox(height: 10),
                        const Text(
                          '* כתובת מייל',
                          style: TextStyle(fontSize: 16),
                        ),
                        const SizedBox(height: 5),
                        TextFormField(
                          controller: emailController,
                          decoration: InputDecoration(
                            hintText: 'הכנס כתובת מייל',
                            filled: true,
                            fillColor: const Color(0xFFB0C4DE),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(10),
                              borderSide: BorderSide.none,
                            ),
                          ),
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'כתובת המייל נדרשת';
                            } else if (!RegExp(
                              r'^[^@]+@[^@]+\.[^@]+',
                            ).hasMatch(value)) {
                              return 'כתובת המייל אינה תקינה';
                            }
                            return null;
                          },
                        ),
                        const SizedBox(height: 10),
                        const Text(
                          '* מספר טלפון',
                          style: TextStyle(fontSize: 16),
                        ),
                        const SizedBox(height: 5),
                        TextFormField(
                          controller: phoneController,
                          decoration: InputDecoration(
                            hintText: 'הכנס מספר טלפון',
                            filled: true,
                            fillColor: const Color(0xFFB0C4DE),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(10),
                              borderSide: BorderSide.none,
                            ),
                          ),
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'מספר הטלפון נדרש';
                            } else if (!RegExp(
                              r'^[0-9]{10}$',
                            ).hasMatch(value)) {
                              return 'מספר הטלפון חייב להכיל 10 ספרות';
                            }
                            return null;
                          },
                        ),
                        const SizedBox(height: 10),
                        const Text('* סיסמה', style: TextStyle(fontSize: 16)),
                        const SizedBox(height: 5),
                        TextFormField(
                          controller: passwordController,
                          obscureText: _obscurePassword,
                          decoration: InputDecoration(
                            hintText: 'הכנס סיסמה',
                            filled: true,
                            fillColor: const Color(0xFFB0C4DE),
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
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'הסיסמה נדרשת';
                            } else if (value.length < 8) {
                              return 'הסיסמה חייבת להכיל לפחות 8 תווים';
                            } else if (!RegExp(
                              r'^[a-zA-Z0-9]+$',
                            ).hasMatch(value)) {
                              return 'הסיסמה יכולה להכיל רק אותיות ומספרים';
                            }
                            return null;
                          },
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 15),
                  ElevatedButton(
                    onPressed: registerUser, // קריאה לפונקציית ההרשמה
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 40,
                        vertical: 15,
                      ),
                    ),
                    child: const Text('הרשמה', style: TextStyle(fontSize: 16)),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
