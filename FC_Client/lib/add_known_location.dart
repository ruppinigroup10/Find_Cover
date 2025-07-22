import 'package:flutter/material.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'base_page.dart'; // ייבוא BasePage
import 'known_location.dart'; // ייבוא עמוד אזורים מוכרים
import 'package:shared_preferences/shared_preferences.dart';

class AddKnownLocationPage extends StatefulWidget {
  const AddKnownLocationPage({super.key});

  @override
  State<AddKnownLocationPage> createState() => _AddKnownLocationPageState();
}

class _AddKnownLocationPageState extends State<AddKnownLocationPage> {
  final TextEditingController addressController = TextEditingController();
  final TextEditingController nameController = TextEditingController();

  @override
  void dispose() {
    addressController.dispose();
    nameController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
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
                _buildTextField('הזן כתובת', 'תאגור 1', addressController),
                const SizedBox(height: 10),
                _buildTextField('שם', '', nameController),
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
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text('שגיאה: לא נמצא מזהה משתמש'),
                          behavior: SnackBarBehavior.floating,
                          margin: EdgeInsets.only(
                            bottom: 120,
                            right: 20,
                            left: 20,
                          ),
                        ),
                      );
                      return;
                    }
                    final providerId = int.parse(providerIdStr);
                    final address = addressController.text.trim();
                    final name = nameController.text.trim();
                    if (address.isEmpty || name.isEmpty) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text('יש למלא את כל השדות'),
                          behavior: SnackBarBehavior.floating,
                          margin: EdgeInsets.only(
                            bottom: 120,
                            right: 20,
                            left: 20,
                          ),
                        ),
                      );
                      return;
                    }
                    // דוגמה: ערכים דיפולטיביים לנתונים גאוגרפיים
                    double latitude = 0.0;
                    double longitude = 0.0;
                    double radius = 0.0;
                    // שליחת הבקשה לשרת
                    final url = Uri.parse(
                      'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/AddKnownLocation',
                    );
                    final body = jsonEncode({
                      'UserId': providerId,
                      'Latitude': latitude,
                      'Longitude': longitude,
                      'Radius': radius,
                      'Address': address,
                      'LocationName': name,
                    });
                    // הדפסות debug
                    print('--- נתונים הנשלחים לשרת ---');
                    print('UserId: $providerId');
                    print('Latitude: $latitude');
                    print('Longitude: $longitude');
                    print('Radius: $radius');
                    print('Address: $address');
                    print('LocationName: $name');
                    print('JSON: $body');
                    print('----------------------------');
                    try {
                      final response = await http.post(
                        url,
                        headers: {'Content-Type': 'application/json'},
                        body: body,
                      );
                      if (response.statusCode == 200) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text('השמירה בוצעה בהצלחה!'),
                            behavior: SnackBarBehavior.floating,
                            margin: EdgeInsets.only(
                              bottom: 120,
                              right: 20,
                              left: 20,
                            ),
                            duration: Duration(seconds: 3),
                          ),
                        );
                        Future.delayed(const Duration(seconds: 3), () {
                          Navigator.pushReplacement(
                            context,
                            MaterialPageRoute(
                              builder: (context) => const KnownLocationPage(),
                            ),
                          );
                        });
                      } else {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text('אירעה שגיאה בשמירה'),
                            behavior: SnackBarBehavior.floating,
                            margin: EdgeInsets.only(
                              bottom: 120,
                              right: 20,
                              left: 20,
                            ),
                          ),
                        );
                      }
                    } catch (e) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text('שגיאת רשת: לא ניתן לשמור כעת'),
                          behavior: SnackBarBehavior.floating,
                          margin: EdgeInsets.only(
                            bottom: 120,
                            right: 20,
                            left: 20,
                          ),
                        ),
                      );
                    }
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

  Widget _buildTextField(
    String label,
    String hint,
    TextEditingController controller,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        TextField(
          controller: controller,
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
    final userId = prefs.getString('user_id') ?? '';
    await prefs.setString('provider_id', userId);
    return userId;
  }
}
