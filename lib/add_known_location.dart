import 'package:flutter/material.dart';
import 'base_page.dart'; // ייבוא BasePage
import 'known_location.dart'; // ייבוא עמוד אזורים מוכרים
import 'package:shared_preferences/shared_preferences.dart';

class AddKnownLocationPage extends StatelessWidget {
  const AddKnownLocationPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20.0),
          child: SingleChildScrollView(
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
                            builder: (context) => const KnownLocationPage(),
                          ),
                        );
                      },
                    ),
                    Image.asset(
                      'assets/images/LOGO1.png',
                      width: 100,
                      height: 100,
                    ),
                    const SizedBox(width: 48),
                  ],
                ),
                const SizedBox(height: 10),
                const Text(
                  'הוספת אזור מוכר',
                  style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 20),
                _buildTextField('הזן כתובת', 'תאגור 1'),
                const SizedBox(height: 10),
                _buildTextField('שם', ''),
                const SizedBox(height: 20),
                Container(
                  height: 200,
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: Colors.grey),
                  ),
                  child: const Center(
                    child: Text(
                      'מפה תוצג כאן',
                      style: TextStyle(color: Colors.grey),
                    ),
                  ),
                ),
                const SizedBox(height: 20),
                ElevatedButton(
                  onPressed: () async {
                    final providerIdStr = await _getProviderId();
                    if (providerIdStr.isEmpty ||
                        int.tryParse(providerIdStr) == null) {
                      print('providerId לא תקין: $providerIdStr');
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text('שגיאה: לא נמצא מזהה משתמש'),
                          margin: EdgeInsets.only(
                            bottom: 50,
                            right: 20,
                            left: 20,
                          ),
                        ),
                      );
                      return;
                    }
                    final providerId = int.parse(providerIdStr);
                    print('providerId: $providerId');

                    // הוסף כאן את הפעולה לשמירת האזור המוכר
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(
                      horizontal: 40,
                      vertical: 15,
                    ),
                  ),
                  child: const Text('שמירה', style: TextStyle(fontSize: 16)),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildTextField(String label, String hint) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        TextField(
          decoration: InputDecoration(
            hintText: hint,
            filled: true,
            fillColor: const Color(0xFFB0C4DE),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide.none,
            ),
          ),
        ),
      ],
    );
  }

  Future<String> _getProviderId() async {
    final prefs = await SharedPreferences.getInstance();
    final providerId = prefs.getString('provider_id') ?? '';
    print('provider_id מהלוקל סטורג\': $providerId');
    return providerId;
  }
}
