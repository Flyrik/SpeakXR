La prise de parole en public est une compétence essentielle tant dans le monde académique que professionnel, mais demeure une source d’anxiété et d’inhibition pour de nombreux apprenants. En l’absence d’un cadre d’entraînement régulier et de retours objectifs, il est difficile de progresser et de gagner en confiance. Parallèlement, les méthodes traditionnelles de formation ; ateliers en présentiel, jeux de rôle, coachings individuels, sont souvent lourdes à déployer, coûteuses et limitées en termes de personnalisation et de fréquence. 

Le projet SPEAKXR propose de relever ces défis en offrant une solution immersive de formation à la prise de parole, fondée sur la réalité virtuelle et l’analyse automatique du discours. Grâce à l’utilisation d’Unity et du casque Meta Quest, l’utilisateur évolue librement dans des environnements virtuels reproduisant des salles de classe ou des amphithéâtres, peuplés d’avatars réalistes animés via Mixamo. En parallèle, l’intégration de l’API Azure Speech SDK permet de transcrire en temps réel la voix de l’orateur et de détecter automatiquement les marqueurs de stress (pauses prolongées, hésitations, répétitions), pour générer à l’issue de chaque session un retour quantifié, objectif et immédiatement exploitable. 

L’objectif de SPEAKXR est triple : 

- Réduire le stress lié à l’exercice de la prise de parole, en proposant un environnement sans jugement et sans formateur présent physiquement. 

- Renforcer la confiance en soi, grâce à des exercices progressifs et des indicateurs de performance clairs. 

- Rendre la formation aux soft skills accessible et autonome, par un dispositif entièrement déployable sur un simple casque VR, avec export des rapports en formats HTML ou TXT. 



Afin de vérifier la fiabilité des indicateurs de stress calculés par SPEAKXR, nous avons mené une expérience comparative. Chaque participant a réalisé un discours dans deux conditions distinctes : 

-Une situation calme sans stress (présentation d'un discours sans la simulation SPEAKXR), 

-Puis une simulation dans SPEAKXR (même discours en VR, avec avatars et bruits d’ambiance). 

À l’issue de chaque session, deux données ont été collectées : 

-Le score de stress généré automatiquement par notre outil (/10), 

-Le ressenti subjectif du participant sur son propre niveau de stress (/10). 

La comparaison entre ces deux valeurs a montré une correspondance moyenne de 89,5 %, ce qui valide la fiabilité du système de détection de stress de SPEAKXR. 
Ce résultat démontre que les indicateurs analysés (pauses longues, hésitations, répétitions) sont pertinents et bien corrélés à l’état émotionnel réel de l’orateur. 







Afin d'avoir accès a l'IA, mettre l'API d'Azure modifier dans SpeechManagerAvBasti :  private string azureApiKey = " ";
