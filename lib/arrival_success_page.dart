import 'package:flutter/material.dart';
import 'package:share_plus/share_plus.dart';
import 'base_page_emergency.dart';
import 'thank_you_page.dart';
import 'settings.dart'; // Ensure this file exports a class named SettingsPage

class ArrivalSuccessPage extends StatelessWidget {
  const ArrivalSuccessPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return BasePage(
      child: Container(
        width: double.infinity,
        height: double.infinity,
        color: const Color(0xFFF7F7F7), // צבע רקע זהה למסך הניווט
        child: Stack(
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                const SizedBox(height: 100),
                Row(
                  children: [
                    const Spacer(),
                    IconButton(
                      icon: const Icon(Icons.arrow_forward_ios),
                      onPressed: () => Navigator.of(context).maybePop(),
                      splashRadius: 24,
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Image.asset('assets/images/LOGO1.png', height: 50),
                  ],
                ),
                const SizedBox(height: 12),
                const Text(
                  '!שמחים שהגעת בשלום',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 22),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 30),
                // הודעה לאנשי הקשר שנגעת למרחב מוגן
                Padding(
                  padding: const EdgeInsets.only(
                    right: 24,
                    left: 24,
                    bottom: 2,
                  ),
                  child: Align(
                    alignment: Alignment.centerRight,
                    child: Text(
                      'הודעה לאנשי קשר שהגעת למרחב מוגן',
                      style: const TextStyle(
                        fontSize: 15,
                        color: Colors.black87,
                      ),
                      textAlign: TextAlign.right,
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 4,
                  ),
                  child: ElevatedButton(
                    onPressed: () async {
                      await Share.share('הגעתי למרחב מוגן בבטחה!');
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      minimumSize: const Size.fromHeight(44),
                    ),
                    child: const Text(
                      '"לשליחת "הגעתי למרחב מוגן',
                      style: TextStyle(
                        fontSize: 18,
                        color: Colors.white,
                        fontWeight: FontWeight.normal,
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 8),
                // ====== כפתור הוקרת תודה (הוסתר לחלוטין לפי בקשת המשתמש) ======
                /*
                Padding(
                  padding: const EdgeInsets.only(
                    right: 24,
                    left: 24,
                    bottom: 2,
                    top: 8,
                  ),
                  child: Align(
                    alignment: Alignment.centerRight,
                    child: Text(
                      'הודעת תודה למארחים',
                      style: const TextStyle(
                        fontSize: 15,
                        color: Colors.black87,
                      ),
                      textAlign: TextAlign.right,
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 4,
                  ),
                  child: ElevatedButton(
                    onPressed: () {
                      Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (context) => const ThankYouPage(),
                        ),
                      );
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      minimumSize: const Size.fromHeight(44),
                    ),
                    child: const Text(
                      'לשליחת תודה',
                      style: TextStyle(
                        fontSize: 18,
                        color: Colors.white,
                        fontWeight: FontWeight.normal,
                      ),
                    ),
                  ),
                ),
                */
                // ====== סוף כפתור הוקרת תודה ======
                // שנחזור לאזעקה הבאה?
                Padding(
                  padding: const EdgeInsets.only(
                    right: 24,
                    left: 24,
                    bottom: 2,
                    top: 8,
                  ),
                  child: Align(
                    alignment: Alignment.centerRight,
                    child: Text(
                      '?שנתכונן לאזעקה הבאה',
                      style: const TextStyle(
                        fontSize: 15,
                        color: Colors.black87,
                      ),
                      textAlign: TextAlign.right,
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 4,
                  ),
                  child: ElevatedButton(
                    onPressed: () {
                      Navigator.of(context).push(
                        MaterialPageRoute(
                          builder:
                              (context) => const SettingsPage(title: 'הגדרות'),
                        ),
                      );
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      minimumSize: const Size.fromHeight(44),
                    ),
                    child: const Text(
                      'לעדכון הגדרות',
                      style: TextStyle(
                        fontSize: 18,
                        color: Colors.white,
                        fontWeight: FontWeight.normal,
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
