import 'Login.dart'; // ייבוא דף ההתחברות
import 'package:flutter/material.dart';
import 'base_page.dart'; // ייבוא BasePage
import 'adding_shelter.dart'; // ייבוא דף AddingShelter
import 'local_storage_service.dart'; // ייבוא שירות אחסון מקומי
import 'settings.dart'; // ייבוא דף ההגדרות

class MyHomePage extends StatefulWidget {
  const MyHomePage({super.key, required this.title, required userData});

  final String title;

  @override
  State<MyHomePage> createState() => _MyHomePageState();
}

class _MyHomePageState extends State<MyHomePage> {
  void navigateToSettings() {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder:
            (context) =>
                const SettingsPage(title: 'הגדרות'), // ניווט לדף ההגדרות
      ),
    );
  }

  void navigateToAddingShelter() {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder:
            (context) =>
                AddingShelterPage(), // ניווט לעמוד Adding_shelter ללא const
      ),
    );
  }

  void logout() async {
    await LocalStorageService.clearUserData();
    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(
        builder: (context) => const LoginPage(), // ניווט למסך ההתחברות
      ),
      (route) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    ButtonStyle style = ElevatedButton.styleFrom(
      backgroundColor: const Color.fromARGB(255, 29, 46, 89),
      foregroundColor: Colors.white,
    );

    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Container(
          width: double.infinity,
          height: double.infinity,
          color: const Color(0xFFF7F7F7), // צבע רקע זהה למסך הניווט
          child: Stack(
            children: [
              Positioned(
                top: 50, // נמוך יותר
                left: 20,
                child: IconButton(
                  icon: const Icon(Icons.logout, color: Colors.red),
                  onPressed: logout, // קריאה לפונקציית ההתנתקות
                ),
              ),
              Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: <Widget>[
                    Container(
                      margin: EdgeInsets.zero, // ביטול כל ה-margin
                      padding: EdgeInsets.zero, // ביטול כל ה-padding
                      child: Column(
                        children: [
                          Image(
                            image: const AssetImage('assets/images/LOGO1.png'),
                            width: 250,
                            height: 200,
                          ),
                          Text(
                            'יישומון חירום למרחב מוגן שיתופי',
                            style: const TextStyle(fontWeight: FontWeight.bold),
                            textAlign: TextAlign.center,
                            textScaleFactor: 1.25,
                            textDirection: TextDirection.rtl,
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 40), // רווח גדול יותר אחרי הלוגו
                    ElevatedButton(
                      onPressed: navigateToSettings, // ניווט לדף ההגדרות
                      style: style,
                      child: const Text(
                        'שנתכונן לאזעקה הבאה?',
                        textDirection: TextDirection.rtl,
                      ),
                    ),
                    const SizedBox(height: 20), // רווח גדול יותר בין הכפתורים
                    ElevatedButton(
                      onPressed:
                          navigateToAddingShelter, // קריאה לפונקציה שמנווטת לעמוד Adding_shelter
                      style: style, // שימוש באותו עיצוב כמו הכפתורים האחרים
                      child: const Text(
                        'שנציל חיים היום?',
                        textDirection: TextDirection.rtl,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
