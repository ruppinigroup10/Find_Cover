import 'package:flutter/material.dart';
import 'base_page.dart'; // ייבוא BasePage
import 'settings.dart'; // ייבוא עמוד ההגדרות

class MySheltersPage extends StatelessWidget {
  const MySheltersPage({super.key});

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
                              (context) => const SettingsPage(title: 'הגדרות'),
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
                'מרחבים מוגנים שלי',
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 20),
              Expanded(
                child: ListView(
                  children: [_buildShelterButton(context, 'בית')],
                ),
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                onPressed: () {
                  // הוסף כאן את הפעולה להוספת מרחב מוגן
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 40,
                    vertical: 15,
                  ),
                ),
                child: const Text(
                  'להוספת מרחב מוגן',
                  style: TextStyle(fontSize: 16),
                ),
              ),
              const SizedBox(height: 375), // ריווח נוסף מתחת לכפתור
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildShelterButton(BuildContext context, String title) {
    return ElevatedButton(
      onPressed: () {
        // הוסף כאן את הפעולה הרצויה לכל כפתור
      },
      style: ElevatedButton.styleFrom(
        backgroundColor: const Color(0xFFB0C4DE),
        foregroundColor: Colors.black,
        padding: const EdgeInsets.symmetric(vertical: 15),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(title, style: const TextStyle(fontSize: 16)), // הטקסט בצד שמאל
          const Icon(Icons.arrow_forward_ios, size: 16), // החץ בצד ימין
        ],
      ),
    );
  }
}
