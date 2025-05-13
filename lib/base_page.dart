import 'package:flutter/material.dart';
import 'add_known_location.dart'; // ייבוא העמוד AddKnownLocationPage

class BasePage extends StatelessWidget {
  final Widget child; // התוכן הייחודי של כל עמוד

  const BasePage({required this.child, super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: child, // התוכן של העמוד
      bottomNavigationBar: Stack(
        clipBehavior: Clip.none, // מאפשר לכפתור לחרוג מהגבולות
        children: [
          BottomAppBar(
            color: const Color(0xFFB0C4DE), // צבע הרקע של ה-BottomAppBar
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: <Widget>[
                ElevatedButton(
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => const AddKnownLocationPage(),
                      ),
                    );
                  },
                  style: ElevatedButton.styleFrom(
                    shape: const CircleBorder(),
                    padding: const EdgeInsets.all(15),
                    foregroundColor: Colors.white,
                    backgroundColor: const Color(0xFFB0C4DE),
                    side: const BorderSide(
                      color: Colors.white, // צבע הגבול
                      width: 2, // עובי הגבול
                    ),
                  ),
                  child: const Text('+'),
                ),
                ElevatedButton(
                  onPressed: () {
                    // פעולה לכפתור הימני
                  },
                  style: ElevatedButton.styleFrom(
                    shape: const CircleBorder(),
                    padding: const EdgeInsets.all(15),
                    foregroundColor: Colors.white,
                    backgroundColor: const Color(0xFFB0C4DE),
                    side: const BorderSide(
                      color: Colors.white, // צבע הגבול
                      width: 2, // עובי הגבול
                    ),
                  ),
                  child: const Text('i'),
                ),
              ],
            ),
          ),
          Positioned(
            top: -30, // מיקום הכפתור מעל ה-BottomAppBar
            left: MediaQuery.of(context).size.width / 2 - 40, // מרכז הכפתור
            child: SizedBox(
              width: 80, // רוחב הכפתור
              height: 80, // גובה הכפתור
              child: ElevatedButton(
                onPressed: () {
                  // פעולה לכפתור המרכזי
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFFB0C4DE),
                  shape: const CircleBorder(),
                  padding: EdgeInsets.zero,
                  foregroundColor: Colors.white,
                  side: const BorderSide(
                    color: Colors.white, // צבע הגבול
                    width: 2, // עובי הגבול
                  ),
                ),
                child: const Text(
                  'משטרה',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(fontSize: 16),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
