import 'package:flutter/material.dart';
import 'package:share_plus/share_plus.dart';
import 'base_page_emergency.dart';

class ThankYouPage extends StatelessWidget {
  const ThankYouPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return BasePage(
      child: Container(
        width: double.infinity,
        height: double.infinity,
        color: const Color(0xFFF7F7F7),
        child: Stack(
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                const SizedBox(height: 100),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    IconButton(
                      icon: const Icon(Icons.arrow_forward_ios),
                      onPressed: () => Navigator.of(context).maybePop(),
                      splashRadius: 24,
                    ),
                  ],
                ),
                // שורה ריקה בין האייקון ללוגו
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Image.asset('assets/images/LOGO1.png', height: 50),
                  ],
                ),
                const SizedBox(height: 12),
                const Text(
                  '!תודה שבחרת לומר תודה',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 20),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 40),
                _ThankButton(
                  text: _fixExclamation('תודה שהכנסת אותי למרחב מוגן שלך'),
                  onPressed: () {},
                ),
                const SizedBox(height: 12),
                _ThankButton(
                  text: _fixExclamation('תודה שהיית שם בשבילי ברגעים של צורך'),
                  onPressed: () {},
                ),
                const SizedBox(height: 12),
                _ThankButton(
                  text: _fixExclamation('תודה שפתחת לי את הדלת'),
                  onPressed: () {},
                ),
                const SizedBox(height: 18),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 24),
                  child: ElevatedButton(
                    onPressed: () async {
                      final result = await showDialog<String>(
                        context: context,
                        builder: (context) {
                          TextEditingController controller =
                              TextEditingController();
                          return AlertDialog(
                            title: const Text(
                              'עריכת הודעה',
                              textAlign: TextAlign.center,
                              style: TextStyle(fontWeight: FontWeight.bold),
                            ),
                            content: SingleChildScrollView(
                              padding: EdgeInsets.only(
                                bottom:
                                    MediaQuery.of(context).viewInsets.bottom,
                              ),
                              child: TextField(
                                controller: controller,
                                maxLines: 3,
                                textDirection: TextDirection.rtl,
                                decoration: const InputDecoration(
                                  border: OutlineInputBorder(),
                                ),
                              ),
                            ),
                            actions: [
                              TextButton(
                                onPressed: () => Navigator.of(context).pop(),
                                child: const Text('ביטול'),
                              ),
                              ElevatedButton(
                                onPressed:
                                    () => Navigator.of(
                                      context,
                                    ).pop(controller.text),
                                child: const Text('שתף'),
                              ),
                            ],
                          );
                        },
                      );
                      if (result != null && result.trim().isNotEmpty) {
                        await Share.share(result);
                      }
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFFD6E3F3),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      minimumSize: const Size.fromHeight(44),
                      elevation: 0,
                    ),
                    child: Row(
                      children: [
                        Transform.rotate(
                          angle: 3.1416,
                          child: const Icon(
                            Icons.send,
                            color: Color(0xFF1D2E59),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Directionality(
                            textDirection: TextDirection.rtl,
                            child: Text(
                              'לכתיבת ברכה אישית',
                              style: const TextStyle(
                                fontSize: 16,
                                color: Color(0xFF1D2E59),
                                fontWeight: FontWeight.normal,
                              ),
                              textAlign: TextAlign.right,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
            // כפתור משטרה בתחתית (הוסר לבקשת המשתמש)
          ],
        ),
      ),
    );
  }
}

class _ThankButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  const _ThankButton({required this.text, this.onPressed});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24),
      child: ElevatedButton(
        onPressed: () async {
          await Share.share(_fixExclamationToEnd(text));
        },
        style: ElevatedButton.styleFrom(
          backgroundColor: const Color(0xFFD6E3F3),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          padding: const EdgeInsets.symmetric(vertical: 12),
          minimumSize: const Size.fromHeight(44),
          elevation: 0,
        ),
        child: Row(
          children: [
            Transform.rotate(
              angle: 3.1416,
              child: const Icon(Icons.send, color: Color(0xFF1D2E59)),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Directionality(
                textDirection: TextDirection.rtl,
                child: Text(
                  _fixExclamationToEnd(text),
                  style: const TextStyle(
                    fontSize: 16,
                    color: Color(0xFF1D2E59),
                    fontWeight: FontWeight.normal,
                  ),
                  textAlign: TextAlign.right,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

String _fixExclamation(String text) {
  // Ensure exclamation mark is at the beginning, not the end
  String trimmed = text.trim();
  if (trimmed.endsWith('!')) {
    trimmed = trimmed.substring(0, trimmed.length - 1).trim();
  }
  if (!trimmed.startsWith('!')) {
    trimmed = '!$trimmed';
  }
  return trimmed;
}

String _fixExclamationToEnd(String text) {
  // Ensure exclamation mark is at the end for RTL
  String trimmed = text.trim();
  if (trimmed.startsWith('!')) {
    trimmed = trimmed.substring(1).trim();
  }
  if (!trimmed.endsWith('!')) {
    trimmed = '$trimmed!';
  }
  return trimmed;
}
