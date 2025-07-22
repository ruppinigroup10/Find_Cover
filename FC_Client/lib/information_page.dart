import 'package:flutter/material.dart';

class InformationPage extends StatelessWidget {
  const InformationPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('אודות Find Cover'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Navigator.of(context).pop(),
        ),
      ),
      body: Container(
        width: double.infinity,
        height: double.infinity,
        color: const Color(0xFFF7F7F7), // צבע רקע זהה למסך הניווט
        child: Center(
          child: SingleChildScrollView(
            child: Padding(
              padding: const EdgeInsets.all(24.0),
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
                child: const Directionality(
                  textDirection: TextDirection.rtl,
                  child: Text(
                    'Find Cover היא אפליקציה שנועדה לסייע לך למצוא מרחב מוגן קרוב בזמן חירום. היא נותנת מענה לצורך חיוני שאינו מקבל מענה מספק כיום: היכולת למצוא מחסה כאשר אתה מחוץ לביתך המוכר.\n\nאיך זה עובד בזמן התראה?\n\nבזמן אזעקה, האפליקציה מאתרת את מיקומך הנוכחי בזמן אמת.\n\nהמערכת מאתרת את המרחב המוגן הקרוב ביותר אליך, בין אם זה מקלט ציבורי או מרחב מוגן פרטי ששיתף על ידי משתמשים אחרים.\n\nהאפליקציה מספקת הנחיות ניווט מהירות ויעילות אל המרחב המוגן שנמצא.\n\nהאפליקציה מתממשקת למערכות ההתראה של פיקוד העורף כדי לודא שתקבל מידע בזמן הנכון.\n\nהאפליקציה נועדה לשפר את תחושת הביטחון, להפחית לחץ ופאניקה ולאפשר לך להתמגן במהירות האפשרית. היא גם שואפת לשפר את הנגישות עבור כל האוכלוסייה, כולל בעלי מוגבלויות ודוברי שפות זרות.',
                    style: TextStyle(
                      fontSize: 18,
                      color: Colors.black87,
                      height: 1.6,
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
