import 'package:flutter/material.dart';
import 'Login.dart'; // ייבוא עמוד ההתחברות
import 'Register.dart'; // ייבוא עמוד ההרשמה
import 'base_before_login.dart'; // ייבוא Base_before_login

class EnterPage extends StatelessWidget {
  const EnterPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Column(
                children: [
                  Image.asset(
                    'assets/images/LOGO1.png', // עדכן את הנתיב לתמונה שלך
                    width: 150,
                    height: 150,
                  ),
                  const SizedBox(height: 10),
                  const Text(
                    'ברוך הבא',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 24),
                  ),
                ],
              ),
              const SizedBox(height: 30),
              ElevatedButton(
                onPressed: () {
                  // מעבר לעמוד ההתחברות
                  Navigator.push(
                    context,
                    MaterialPageRoute(builder: (context) => const LoginPage()),
                  );
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 40,
                    vertical: 15,
                  ),
                ),
                child: const Text('התחברות', style: TextStyle(fontSize: 16)),
              ),
              const SizedBox(height: 10),
              GestureDetector(
                onTap: () {
                  // פעולה לכפתור "הרשם עכשיו"
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder:
                          (context) =>
                              RegisterPage(), // הסרתי const כי RegisterPage אינו קונסטרקטור const
                    ), // מעבר לעמוד ההרשמה
                  );
                },
                child: MouseRegion(
                  cursor:
                      SystemMouseCursors.click, // שינוי סמן העכבר לסמן של קישור
                  child: const Text(
                    'אין לך משתמש עדיין? הרשם עכשיו!',
                    style: TextStyle(
                      color: Colors.blue,
                      // decoration: TextDecoration.underline, // קו תחתון לטקסט
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 30),
            ],
          ),
        ),
      ),
    );
  }
}
