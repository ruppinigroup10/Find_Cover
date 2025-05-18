import 'package:fc_flutter/settings.dart'; // ייבוא דף ההגדרות
import 'package:fc_flutter/login.dart'; // ייבוא דף ההתחברות
import 'package:flutter/material.dart';
import 'base_page.dart'; // ייבוא BasePage
import 'adding_shelter.dart'; // ייבוא דף AddingShelter

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

  void logout() {
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(
        builder: (context) => const LoginPage(), // ניווט למסך ההתחברות
      ),
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
        child: Stack(
          children: [
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
                  const SizedBox(height: 20),
                  ElevatedButton(
                    onPressed: navigateToSettings, // ניווט לדף ההגדרות
                    style: style,
                    child: const Text(
                      'שנתכונן לאזעקה הבאה?',
                      textDirection: TextDirection.rtl,
                    ),
                  ),
                  const SizedBox(height: 10), // רווח בין הכפתורים
                  ElevatedButton(
                    onPressed:
                        navigateToAddingShelter, // קריאה לפונקציה שמנווטת לעמוד Adding_shelter
                    style: style, // שימוש באותו עיצוב כמו הכפתורים האחרים
                    child: const Text(
                      'שנציל חיים היום?',
                      textDirection: TextDirection.rtl,
                    ),
                  ),
                  const SizedBox(height: 10), // רווח בין הכפתורים
                  ElevatedButton(
                    onPressed: () {
                      // פעולה אחרת או השארת הכפתור ללא פעולה
                      print('כפתור "שנתכנן מסלול?" נלחץ');
                    },
                    style: style,
                    child: const Text(
                      'שנתכנן מסלול?',
                      textDirection: TextDirection.rtl,
                    ),
                  ),
                ],
              ),
            ),
            Positioned(
              top: 20,
              left: 20,
              child: IconButton(
                icon: const Icon(Icons.logout, color: Colors.red),
                onPressed: logout, // קריאה לפונקציית ההתנתקות
              ),
            ),
          ],
        ),
      ),
    );
  }
}
