import 'package:flutter/material.dart';
import 'base_page.dart'; // ייבוא BasePage
import 'personal_details.dart'; // ייבוא עמוד פרטים אישיים
import 'known_location.dart'; // ייבוא עמוד אזורים מוכרים
import 'history_of_protected_areas.dart'; // ייבוא עמוד היסטוריית מרחבים מוגנים
import 'my_shelters.dart'; // ייבוא עמוד המרחבים המוגנים שלי
import 'home_page.dart'; // ייבוא עמוד הבית

class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key, required String title});

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20.0),
          child: Column(
            children: [
              const SizedBox(height: 20),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_ios),
                    onPressed: () {
                      Navigator.pushReplacement(
                        context,
                        MaterialPageRoute(
                          builder:
                              (context) => MyHomePage(
                                title: 'מסך הבית',
                                userData: null,
                              ), // Replace with actual user data if available
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
                'הגדרות',
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 20),
              Expanded(
                child: ListView(
                  shrinkWrap: true, // מתאים את הגובה לתוכן
                  children: [
                    _buildSettingsButton(context, 'פרטים אישיים'),
                    const SizedBox(height: 10), // רווח בין הכפתורים
                    _buildSettingsButton(context, 'אזורים מוכרים'),
                    const SizedBox(height: 10), // רווח בין הכפתורים
                    _buildSettingsButton(context, 'היסטוריית מרחבים מוגנים'),
                    const SizedBox(height: 10), // רווח בין הכפתורים
                    _buildSettingsButton(context, 'המרחבים המוגנים שלי'),
                    const SizedBox(height: 10), // רווח בין הכפתורים
                    _buildSettingsButton(context, 'תנאי שימוש'),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSettingsButton(BuildContext context, String title) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5.0),
      child: ElevatedButton(
        onPressed: () {
          if (title == 'פרטים אישיים') {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (context) => PersonalDetailsPage(
                  username: 'exampleUsername', // Replace with actual username
                  email: 'example@example.com', // Replace with actual email
                  phoneNumber: '1234567890', // Replace with actual phone number
                ),
              ),
            );
          } else if (title == 'אזורים מוכרים') {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (context) => const KnownLocationPage(),
              ),
            );
          } else if (title == 'היסטוריית מרחבים מוגנים') {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (context) => const HistoryOfProtectedAreasPage(),
              ),
            );
          } else if (title == 'המרחבים המוגנים שלי') {
            Navigator.push(
              context,
              MaterialPageRoute(builder: (context) => const MySheltersPage()),
            );
          } else {
            // ניתן להוסיף פעולות נוספות לכפתורים אחרים
          }
        },
        style: ElevatedButton.styleFrom(
          backgroundColor: const Color(0xFFB0C4DE),
          foregroundColor: Colors.black,
          padding: const EdgeInsets.symmetric(vertical: 15),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(title, style: const TextStyle(fontSize: 16)), // הטקסט בצד שמאל
            const Icon(Icons.arrow_forward_ios, size: 16), // החץ בצד ימין
          ],
        ),
      ),
    );
  }
}
