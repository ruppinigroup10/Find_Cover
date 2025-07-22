import 'package:flutter/material.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import 'base_page.dart'; // ייבוא BasePage

class AddingShelterPage extends StatefulWidget {
  final int? shelterId; // הוסף פרמטר אופציונלי
  const AddingShelterPage({super.key, this.shelterId});

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
    // Try all possible keys in order of reliability
    String? providerId = prefs.getString('UserId');
    if (providerId == null || providerId.isEmpty) {
      providerId = prefs.getString('user_id');
    }
    if (providerId == null || providerId.isEmpty) {
      providerId = prefs.getString('provider_id');
    }
    return providerId ?? '';
  }

  Future<void> printProviderIdFromLocalStorage() async {
    final prefs = await SharedPreferences.getInstance();
    final providerId = prefs.getString('provider_id') ?? '';
    print('provider_id מהלוקל סטורג\': $providerId');
  }

  @override
  void initState() {
    super.initState();
    if (widget.shelterId != null) {
      _fetchAndFillShelterFromServer(widget.shelterId!);
    }
  }

  Future<void> _fetchAndFillShelterFromServer(int shelterId) async {
    try {
      final response = await http.get(
        Uri.parse(
          'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/getShelter?shelter_id=$shelterId',
        ),
      );
      if (response.statusCode == 200) {
        final decoded = jsonDecode(response.body);
        final shelter =
            decoded['shlter'] ??
            decoded['shelter'] ??
            decoded['Shelter'] ??
            decoded['ShelterData'];
        if (shelter != null) {
          _addressController.text = (shelter['address'] ?? '').toString();
          _nameController.text = (shelter['name'] ?? '').toString();
          _capacityController.text = (shelter['capacity'] ?? '').toString();
          _additionalInfoController.text =
              (shelter['additional_information'] ?? '').toString();
          _isAccessible =
              (shelter['is_accessible'] == true ||
                      shelter['is_accessible'] == 'true' ||
                      shelter['is_accessible'] == 1)
                  ? 'כן'
                  : 'לא';
          _allowPets =
              (shelter['pets_friendly'] == true ||
                      shelter['pets_friendly'] == 'true' ||
                      shelter['pets_friendly'] == 1)
                  ? 'כן'
                  : 'לא';
          setState(() {});
        }
      }
    } catch (e) {
      // טיפול בשגיאה
    }
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
                    Text(
                      widget.shelterId != null
                          ? 'עריכת מרחב מוגן'
                          : 'הוספת מרחב מוגן',
                      style: const TextStyle(
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
                          // הסר בדיקה זו - מגבלת האורך היא 50 תווים בלבד
                          // return;
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
                        if (name.length > 50) {
                          print('שגיאת אורך: שם לא יכול להכיל יותר מ-50 תווים');
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('שם לא יכול להכיל יותר מ-50 תווים'),
                            ),
                          );
                          return;
                        }
                        final capacity =
                            int.tryParse(_capacityController.text) ?? 0;
                        final additionalInfo =
                            _additionalInfoController.text.trim();
                        final providerId = await _getProviderId();
                        print('providerId שנשלח: $providerId');
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
                          'createdAt':
                              DateTime.now()
                                  .toUtc()
                                  .add(const Duration(hours: 3))
                                  .toIso8601String(),
                          'lastUpdated':
                              DateTime.now()
                                  .toUtc()
                                  .add(const Duration(hours: 3))
                                  .toIso8601String(),
                        };
                        // Consolidated single debug printout for all values sent to the server
                        print(
                          '--- כל הערכים שנשלחים לשרת בעת הוספת מרחב מוגן ---',
                        );
                        shelterData.forEach((k, v) => print('  $k: $v'));
                        print('-------------------------------');
                        try {
                          final response = await http.post(
                            Uri.parse(
                              'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/AddShelter',
                            ),
                            headers: {'Content-Type': 'application/json'},
                            body: jsonEncode(shelterData),
                          );
                          if (response.statusCode == 200) {
                            // שמירת נתוני משתמש בלוקל סטורג' לאחר הוספה מוצלחת
                            try {
                              final prefs =
                                  await SharedPreferences.getInstance();
                              final shelterResp = jsonDecode(response.body);
                              if (shelterResp['shelter'] != null &&
                                  shelterResp['shelter']['providerId'] !=
                                      null) {
                                await prefs.setString(
                                  'user_id',
                                  shelterResp['shelter']['providerId']
                                      .toString(),
                                );
                              }
                            } catch (e) {
                              print(
                                'שגיאה בשמירת נתוני משתמש בלוקל סטורג\' אחרי הוספת מרחב מוגן: $e',
                              );
                            }
                            print('המרחב נוסף בהצלחה!');
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(
                                content: Text('המרחב נוסף בהצלחה!'),
                                backgroundColor: Colors.green,
                              ),
                            );
                            Navigator.pop(
                              context,
                              true,
                            ); // החזר true כדי שעמוד מרחבים מוגנים שלי יתרענן
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
                          Text(
                            widget.shelterId != null ? 'עדכון מקלט' : 'הוספה',
                            style: const TextStyle(fontSize: 16),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(
                      height: 5,
                    ), // ריווח 5 פיקסל מתחת לכפתור עדכון/הוספה
                    if (widget.shelterId != null)
                      GestureDetector(
                        onTap: () async {
                          final confirmed = await showDialog<bool>(
                            context: context,
                            builder:
                                (context) => AlertDialog(
                                  title: const Text('אישור מחיקה'),
                                  content: const Text(
                                    'האם אתה בטוח שברצונך למחוק את המקלט?',
                                  ),
                                  actions: [
                                    TextButton(
                                      onPressed:
                                          () =>
                                              Navigator.of(context).pop(false),
                                      child: const Text('ביטול'),
                                    ),
                                    TextButton(
                                      onPressed:
                                          () => Navigator.of(context).pop(true),
                                      child: const Text('אישור'),
                                    ),
                                  ],
                                ),
                          );
                          if (confirmed == true) {
                            final providerId = await _getProviderId();
                            final shelterId = widget.shelterId;
                            final url = Uri.parse(
                              'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/Shelter/DeleteShelter?shelter_id=$shelterId&provider_id=$providerId',
                            );
                            try {
                              final response = await http.delete(url);
                              if (response.statusCode == 200) {
                                ScaffoldMessenger.of(context).showSnackBar(
                                  const SnackBar(
                                    content: Text('המקלט נמחק בהצלחה!'),
                                    backgroundColor: Colors.red,
                                  ),
                                );
                                Navigator.pop(
                                  context,
                                  true,
                                ); // חזור אחורה ורענן רשימה
                              } else {
                                String msg = 'שגיאה במחיקה';
                                try {
                                  final decoded = jsonDecode(response.body);
                                  if (decoded is Map &&
                                      decoded['message'] != null) {
                                    msg = decoded['message'];
                                  }
                                } catch (_) {}
                                ScaffoldMessenger.of(
                                  context,
                                ).showSnackBar(SnackBar(content: Text(msg)));
                              }
                            } catch (e) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(
                                  content: Text('שגיאת חיבור לשרת'),
                                ),
                              );
                            }
                          }
                        },
                        child: const Text(
                          'מחיקת מקלט',
                          style: TextStyle(
                            color: Colors.red,
                            decoration: TextDecoration.underline,
                            decorationColor: Colors.red,
                            decorationThickness: 2,
                            fontSize: 15,
                            fontWeight: FontWeight.normal,
                          ),
                          textAlign: TextAlign.center,
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
                  Navigator.pop(context);
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
          height: 52, // גובה השדה
          width: double.infinity, // תופס את כל הרוחב של העמודה
          child: TextField(
            controller: controller,
            maxLength: maxLength, // הגבלת מספר תווים
            style: const TextStyle(
              fontSize: 14, // שמור על גודל טקסט 14
              color: Color.fromARGB(255, 29, 46, 89),
            ),
            decoration: InputDecoration(
              hintText: hint,
              hintStyle: const TextStyle(
                fontSize: 14,
                color: Color.fromARGB(255, 29, 46, 89),
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
          style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        SizedBox(
          height: 52, // גובה השדה
          width: double.infinity, // תופס את כל הרוחב של העמודה
          child: DropdownButtonFormField<String>(
            value: selectedValue,
            isExpanded: true,
            decoration: InputDecoration(
              filled: true,
              fillColor: const Color(0xFFB0C4DE),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: BorderSide.none,
              ),
            ),
            style: const TextStyle(
              fontSize: 14, // שמור על גודל טקסט 14
              color: Color.fromARGB(255, 29, 46, 89),
            ),
            items:
                options
                    .map(
                      (option) => DropdownMenuItem(
                        value: option,
                        child: Row(
                          children: [
                            Expanded(
                              child: Text(
                                option,
                                textAlign: TextAlign.right,
                                overflow: TextOverflow.ellipsis,
                                maxLines: 1,
                              ),
                            ),
                          ],
                        ),
                      ),
                    )
                    .toList(),
            onChanged: onChanged,
          ),
        ),
      ],
    );
  }
}
