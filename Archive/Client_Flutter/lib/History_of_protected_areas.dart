import 'package:flutter/material.dart';
import 'base_page.dart'; // ייבוא BasePage
import 'settings.dart'; // ייבוא עמוד ההגדרות

class HistoryOfProtectedAreasPage extends StatelessWidget {
  const HistoryOfProtectedAreasPage({super.key});

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
                'היסטוריית מרחבים מוגנים',
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 20),
              Expanded(
                child: ListView(
                  children: [
                    _buildHistoryCard(
                      date: '13/01/2025',
                      area: 'אזור התרעה: חיפה מערב',
                      shelter: 'מרחב מוגן: ממ"ד, בניין 5, מקלט צפוני',
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

  Widget _buildHistoryCard({
    required String date,
    required String area,
    required String shelter,
  }) {
    return Card(
      color: const Color(0xFFB0C4DE),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      child: Padding(
        padding: const EdgeInsets.all(15.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'תאריך: $date',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 5),
            Text(area, style: const TextStyle(fontSize: 14)),
            const SizedBox(height: 5),
            Text(shelter, style: const TextStyle(fontSize: 14)),
          ],
        ),
      ),
    );
  }
}
