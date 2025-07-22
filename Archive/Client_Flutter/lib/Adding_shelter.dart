import 'package:flutter/material.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import 'base_page.dart'; // ייבוא BasePage
import 'settings.dart'; // ייבוא עמוד ההגדרות

class AddingShelterPage extends StatefulWidget {
  AddingShelterPage({super.key});

  @override
  State<AddingShelterPage> createState() => _AddingShelterPageState();
}

class _AddingShelterPageState extends State<AddingShelterPage> {
  final TextEditingController _addressController = TextEditingController();
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _capacityController = TextEditingController();
  final TextEditingController _additionalInfoController =
      TextEditingController();
  String _isAccessible = 'לא';
  String _allowPets = 'לא';

  Future<String> _getProviderId() async {
    final prefs = await SharedPreferences.getInstance();
    final providerId = prefs.getString('provider_id') ?? '';
    return providerId;
  }

  Future<void> printProviderIdFromLocalStorage() async {
    final prefs = await SharedPreferences.getInstance();
    final providerId = prefs.getString('provider_id') ?? '';
    print('provider_id מהלוקל סטורג\': $providerId');
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // הגדרת כיוון כל העמוד ל-RTL
      child: BasePage(
        child: Stack(
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20.0),
              child: SingleChildScrollView(
                child: Column(
                  children: [
                    const SizedBox(height: 20),
                    Image.asset(
                      'assets/images/LOGO1.png', // עדכן את הנתיב לתמונה שלך
                      width: 100,
                      height: 100,
                    ),
                    const SizedBox(height: 10),
                    const Text(
                      'הוספת מרחב מוגן',
                      style: TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 20),
                    _buildTextField(
                      'הזן כתובת',
                      'עיר, רחוב, מספר בית',
                      controller: _addressController,
                      maxLength: 50,
                    ),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildTextField(
                      'שם',
                      'הבית של דני',
                      controller: _nameController,
                    ),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildTextField(
                      'תפוסה מקסימלית',
                      '7',
                      controller: _capacityController,
                    ),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildDropdownField('לאפשר כניסת חיות?', ['לא', 'כן'], (
                      value,
                    ) {
                      setState(() {
                        _allowPets = value!;
                      });
                    }),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildDropdownField('קיימת נגישות?', ['לא', 'כן'], (value) {
                      setState(() {
                        _isAccessible = value!;
                      });
                    }),
                    const SizedBox(height: 8), // ריווח קטן יותר
                    _buildTextField(
                      'הערות?',
                      '',
                      maxLength: 255,
                      controller: _additionalInfoController,
                    ),
                    const SizedBox(height: 15), // ריווח קטן יותר לפני הכפתור
                    ElevatedButton(
                      onPressed: () async {
                        // בדיקת שדות חובה
                        if (_nameController.text.trim().isEmpty) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('יש להזין שם מרחב מוגן'),
                            ),
                          );
                          return;
                        }
                        if (_addressController.text.trim().isEmpty) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('יש להזין כתובת')),
                          );
                          return;
                        }
                        if (_capacityController.text.trim().isEmpty) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('יש להזין תפוסה מקסימלית'),
                            ),
                          );
                          return;
                        }
                        if (int.tryParse(_capacityController.text) == null) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('תפוסה מקסימלית חייבת להיות מספר'),
                            ),
                          );
                          return;
                        }
                        // בדיקת מגבלות אורך
                        if (_nameController.text.length > 10) {
                          print('שגיאת אורך: שם לא יכול להכיל יותר מ-10 תווים');
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('שם לא יכול להכיל יותר מ-10 תווים'),
                            ),
                          );
                          return;
                        }
                        if (_addressController.text.length > 50) {
                          print(
                            'שגיאת אורך: כתובת לא יכולה להכיל יותר מ-50 תווים',
                          );
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text(
                                'כתובת לא יכולה להכיל יותר מ-50 תווים',
                              ),
                            ),
                          );
                          return;
                        }
                        if (_additionalInfoController.text.length > 255) {
                          print(
                            'שגיאת אורך: הערות לא יכולות להכיל יותר מ-255 תווים',
                          );
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text(
                                'הערות לא יכולות להכיל יותר מ-255 תווים',
                              ),
                            ),
                          );
                          return;
                        }
                        final shelterType = 'פרטי';
                        if (shelterType.length > 10) {
                          print(
                            'שגיאת אורך: סוג המרחב לא יכול להכיל יותר מ-10 תווים',
                          );
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text(
                                'סוג המרחב לא יכול להכיל יותר מ-10 תווים',
                              ),
                            ),
                          );
                          return;
                        }
                        // קריאה לשרת להוספת מרחב מוגן
                        final address = _addressController.text.trim();
                        final name = _nameController.text.trim();
                        final capacity =
                            int.tryParse(_capacityController.text) ?? 0;
                        final additionalInfo =
                            _additionalInfoController.text.trim();
                        final providerId = await _getProviderId();
                        print('providerId שנשלח: ' + providerId);
                        final isAccessible = _isAccessible == 'כן';
                        final allowPets = _allowPets == 'כן';
                        final shelterData = {
                          'shelterType': shelterType,
                          'name': name,
                          'address': address,
                          'capacity': capacity,
                          'additionalInformation': additionalInfo,
                          'providerId':
                              int.tryParse(providerId) != null
                                  ? int.parse(providerId)
                                  : providerId,
                          'isAccessible': isAccessible,
                          'petsFriendly': allowPets,
                          'isActive': true,
                          'createdAt': DateTime.now().toIso8601String(),
                          'lastUpdated': DateTime.now().toIso8601String(),
                        };
                        try {
                          final response = await http.post(
                            Uri.parse(
                              'http://10.0.2.2:7203/api/Shelter/AddShelter',
                            ),
                            headers: {'Content-Type': 'application/json'},
                            body: jsonEncode(shelterData),
                          );
                          if (response.statusCode == 200) {
                            print('המרחב נוסף בהצלחה!');
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(
                                content: Text('המרחב נוסף בהצלחה!'),
                              ),
                            );
                            Navigator.pop(context);
                          } else {
                            String msg = 'שגיאה בהוספה';
                            try {
                              final decoded = jsonDecode(response.body);
                              if (decoded is Map &&
                                  decoded['message'] != null) {
                                msg = decoded['message'];
                              }
                            } catch (_) {}
                            print('שגיאת שרת: $msg');
                            ScaffoldMessenger.of(
                              context,
                            ).showSnackBar(SnackBar(content: Text(msg)));
                          }
                        } catch (e) {
                          print('שגיאת חיבור לשרת: $e');
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('שגיאת חיבור לשרת')),
                          );
                        }
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color.fromARGB(
                          255,
                          29,
                          46,
                          89,
                        ), // כחול כהה
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(
                          horizontal: 20,
                          vertical: 10,
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const SizedBox(width: 8), // ריווח בין הסמל לטקסט
                          const Text('הוספה', style: TextStyle(fontSize: 16)),
                        ],
                      ),
                    ),
                    const SizedBox(height: 20), // ריווח נוסף מתחת לכפתור
                  ],
                ),
              ),
            ),
            Positioned(
              top: 20,
              right: 20,
              child: IconButton(
                icon: const Icon(Icons.arrow_back_ios),
                onPressed: () {
                  Navigator.pushReplacement(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const SettingsPage(title: 'הגדרות'),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTextField(
    String label,
    String hint, {
    int? maxLength,
    TextEditingController? controller,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.bold,
          ), // גודל טקסט קטן יותר
        ),
        const SizedBox(height: 5),
        SizedBox(
          height: 40, // גובה קטן יותר לשדה
          child: TextField(
            controller: controller,
            maxLength: maxLength, // הגבלת מספר תווים
            style: const TextStyle(
              fontSize: 12,
              color: Color.fromARGB(255, 29, 46, 89), // כחול כהה לטקסט
            ),
            decoration: InputDecoration(
              hintText: hint,
              hintStyle: const TextStyle(
                fontSize: 12,
                color: Color.fromARGB(255, 29, 46, 89), // כחול כהה לרמז
              ),
              filled: true,
              fillColor: const Color(0xFFB0C4DE),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: BorderSide.none,
              ),
              counterText: '', // הסתרת מונה התווים
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildDropdownField(
    String label,
    List<String> options,
    ValueChanged<String?> onChanged,
  ) {
    String? selectedValue;
    if (label == 'לאפשר כניסת חיות?') {
      selectedValue = _allowPets;
    } else if (label == 'קיימת נגישות?') {
      selectedValue = _isAccessible;
    } else {
      selectedValue = options.first;
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.bold,
          ), // גודל טקסט קטן יותר
        ),
        const SizedBox(height: 5),
        SizedBox(
          height: 40, // גובה קטן יותר לשדה
          child: DropdownButtonFormField<String>(
            value: selectedValue,
            decoration: InputDecoration(
              filled: true,
              fillColor: const Color(0xFFB0C4DE),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: BorderSide.none,
              ),
            ),
            style: const TextStyle(
              fontSize: 12,
              color: Color.fromARGB(255, 29, 46, 89), // כחול כהה לטקסט
            ),
            items:
                options
                    .map(
                      (option) => DropdownMenuItem(
                        value: option,
                        child: Text(
                          option,
                          style: const TextStyle(
                            fontSize: 12,
                            color: Color.fromARGB(
                              255,
                              29,
                              46,
                              89,
                            ), // כחול כהה לטקסט
                          ),
                        ),
                      ),
                    )
                    .toList(),
            onChanged: (value) {
              onChanged(value);
            },
          ),
        ),
      ],
    );
  }
}
