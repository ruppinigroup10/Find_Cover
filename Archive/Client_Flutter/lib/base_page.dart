import 'package:flutter/material.dart';
import 'Adding_shelter.dart'; // ייבוא העמוד
// import 'add_known_location_page.dart'; // Uncomment and use if you meant AddKnownLocationPage

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
                        builder: (context) => AddingShelterPage(),
                        // If you meant AddKnownLocationPage, use:
                        // builder: (context) => const AddKnownLocationPage(),
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
                    showDialog(
                      context: context,
                      builder:
                          (context) => Dialog(
                            backgroundColor: Colors.transparent,
                            child: Container(
                              decoration: BoxDecoration(
                                color: Colors.blue[50],
                                borderRadius: BorderRadius.circular(24),
                                boxShadow: [
                                  BoxShadow(
                                    color: Colors.black12,
                                    blurRadius: 8,
                                    offset: Offset(0, 4),
                                  ),
                                ],
                              ),
                              padding: const EdgeInsets.all(20),
                              child: SingleChildScrollView(
                                child: Column(
                                  mainAxisSize: MainAxisSize.min,
                                  crossAxisAlignment:
                                      CrossAxisAlignment.stretch,
                                  children: [
                                    Align(
                                      alignment: Alignment.topLeft,
                                      child: IconButton(
                                        icon: const Icon(Icons.close),
                                        onPressed:
                                            () => Navigator.of(context).pop(),
                                      ),
                                    ),
                                    const Directionality(
                                      textDirection: TextDirection.rtl,
                                      child: Text(
                                        'Find Cover" היא אפליקציה שנועדה לסייע לך למצוא מרחב מוגן קרוב בזמן חירום. היא נותנת מענה לצורך חיוני שאינו מקבל מענה מספק כיום: היכולת למצוא מחסה כאשר אתה מחוץ לסביבתך המוכרת.\n\nאיך זה עובד בזמן התרעה?\n•\nבזמן אזעקה, האפליקציה מאתרת את מיקומך הנוכחי בזמן אמת.\n•\nהמערכת מאתרת את המרחב המוגן הקרוב ביותר אליך, בין אם זה מקלט ציבורי או מרחב מוגן פרטי ששותף על ידי משתמשים אחרים.\n•\nהאפליקציה מספקת הנחיות ניווט מהירות ויעילות אל המרחב המוגן שנמצא.\n•\nהאפליקציה מתממשקת למערכות ההתרעה של פיקוד העורף כדי לוודא שתקבל מידע בזמן הנכון.\n\nהאפליקציה נועדה לשפר את תחושת הביטחון, להפחית לחץ ופאניקה ולאפשר לך להתמגן במהירות האפשרית. היא גם שואפת לשפר את הנגישות עבור כלל האוכלוסייה, כולל בעלי מוגבלויות ודוברי שפות זרות.',
                                        style: TextStyle(
                                          fontSize: 18,
                                          color: Colors.black87,
                                          height: 1.6,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
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
