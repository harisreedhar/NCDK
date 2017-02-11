/* Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using NCDK.Dict;
using NCDK.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace NCDK.QSAR
{
    /// <summary>
    /// A class that provides access to automatic descriptor calculation and more.
    /// <para>
    /// The aim of this class is to provide an easy to use interface to automatically evaluate
    /// all the CDK descriptors for a given molecule. Note that at a given time this class
    /// will evaluate all <i>atomic</i> or <i>molecular</i> descriptors but not both.
    /// </para>
    /// <para>
    /// The available descriptors are determined by scanning all the jar files in the users CLASSPATH
    /// and selecting classes that belong to the CDK QSAR atomic or molecular descriptors package.
    /// </para>
    /// </summary>
    /// <example>
    /// An example of its usage would be
    /// <code>
    /// Molecule someMolecule;
    /// ...
    /// DescriptorEngine descriptoEngine = new DescriptorEngine(DescriptorEngine.MOLECULAR, null);
    /// descriptorEngine.Process(someMolecule);
    /// </code>
    /// <para>
    /// The class allows the user to obtain a List of all the available descriptors in terms of their
    /// Java class names as well as instances of each descriptor class.   For each descriptor, it is possible to
    /// obtain its classification as described in the CDK descriptor-algorithms OWL dictionary.
    /// </para>
    /// </example>
    /// @cdk.created 2004-12-02
    /// @cdk.module qsarmolecular
    /// @cdk.githash
    /// @see DescriptorSpecification
    /// @see Dictionary
    /// @see org.openscience.cdk.dict.OWLFile
    public class DescriptorEngine
    {
        private static readonly XNamespace rdfNS = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        private DictionaryMap dict = null;
        private IList<string> classNames = new List<string>(200);
        private IList<IDescriptor> descriptors = new List<IDescriptor>(200);
        private IList<IImplementationSpecification> speclist = null;

        /// <summary>
        /// readonly
        /// </summary>
        private IChemObjectBuilder builder { get; set; }

        private DescriptorEngine()
        { }

        /// <summary>
        /// Instantiates the DescriptorEngine.
        /// </summary>
        /// <example>
        /// This constructor instantiates the engine but does not perform any initialization. As a result calling
        /// the <c>Process()</c> method will fail. To use the engine via this constructor you should use
        /// the following code
        /// <code>
        /// List classNames = DescriptorEngine.GetDescriptorClassNameByPackage("NCDK.Descriptors.Moleculars", null);
        /// DescriptorEngine engine = DescriptorEngine(classNames);
        /// 
        /// List instances =  engine.InstantiateDescriptors(classNames);
        /// List specs = engine.InitializeSpecifications(instances)
        /// engine.SetDescriptorInstances(instances);
        /// engine.SetDescriptorSpecifications(specs);
        /// 
        /// engine.Process(someAtomContainer);
        /// </code>
        /// This approach allows one to use find classes using the interface based approach (<see cref="GetDescriptorClassNameByInterface(string, string[])"/>.
        /// If you use this method it is preferable to specify the jar files to examine
        /// </example>
        public DescriptorEngine(IList<string> classNames, IChemObjectBuilder builder)
        {
            this.classNames = classNames;
            this.builder = builder;
            descriptors = InstantiateDescriptors(classNames);
            speclist = InitializeSpecifications(descriptors).ToList();

            // get the dictionary for the descriptors
            DictionaryDatabase dictDB = new DictionaryDatabase();
            dict = dictDB.GetDictionary("descriptor-algorithms");
        }

        /// <summary>
        /// Create a descriptor engine for all descriptor types. Descriptors are
        /// loaded using the service provider mechanism. To include custom
        /// descriptors one should declare in {@code META-INF/services} a file named
        /// as the interface you are providing (e.g. <see cref="IMolecularDescriptor"/>).
        /// This file declares the implementations provided by the jar as class names.
        /// </summary>
        /// <typeparam name="T">class of the descriptor to use (<see cref="IMolecularDescriptor"/>)</typeparam>
        /// <returns></returns>
        /// <remarks>
        /// <a href="http://docs.oracle.com/javase/tutorial/sound/SPI-intro.html">Service Provider Interface (SPI) Introduction</a>
        /// </remarks>
        public static DescriptorEngine Create<T>(IChemObjectBuilder builder) where T : IDescriptor
        {
            var o = new DescriptorEngine();
            foreach (var descriptor in ServiceLoader<T>.Load())
            {
                descriptor.Initialise(builder);
                o.descriptors.Add(descriptor);
                o.classNames.Add(descriptor.GetType().FullName);
            }

            o.builder = builder;
            o.speclist = o.InitializeSpecifications(o.descriptors).Cast<IImplementationSpecification>().ToList();
            Debug.WriteLine($"Found #descriptors: {o.classNames.Count}");

            // get the dictionary for the descriptors
            DictionaryDatabase dictDB = new DictionaryDatabase();
            o.dict = dictDB.GetDictionary("descriptor-algorithms");

            return o;
        }

        /// <summary>
        /// Calculates all available (or only those specified) descriptors for a molecule.
        /// <para>
        /// The results for a given descriptor as well as associated parameters and
        /// specifications are used to create a <see cref="DescriptorValue"/>
        /// object which is then added to the molecule as a property keyed
        /// on the <see cref="DescriptorSpecification"/> object for that descriptor</para>
        /// </summary>
        /// <param name="molecule">The molecule for which we want to calculate descriptors</param>
        /// <exception cref="CDKException">if an error occured during descriptor calculation or the descriptors and/or specifications have not been initialized</exception>
        public void Process(IAtomContainer molecule)
        {
            if (descriptors == null || speclist == null)
                throw new CDKException("Descriptors have not been instantiated");
            if (speclist.Count != descriptors.Count)
                throw new CDKException("Number of specs and descriptors do not match");

            for (int i = 0; i < descriptors.Count; i++)
            {
                IDescriptor descriptor = descriptors[i];
                if (descriptor is IMolecularDescriptor)
                {
                    DescriptorValue value = ((IMolecularDescriptor)descriptor).Calculate(molecule);
                    if (value.GetException() == null)
                        molecule.SetProperty(speclist[i], value);
                    else
                    {
                        Trace.TraceError($"Could not calculate descriptor value for: {descriptor.GetType().FullName}");
                        Debug.WriteLine(value.GetException());
                    }
                    Debug.WriteLine("Calculated molecular descriptors...");
                }
                else if (descriptor is IAtomicDescriptor)
                {
                    foreach (var atom in molecule.Atoms)
                    {
                        DescriptorValue value = ((IAtomicDescriptor)descriptor).Calculate(atom, molecule);
                        if (value.GetException() == null)
                            atom.SetProperty(speclist[i], value);
                        else
                        {
                            Trace.TraceError("Could not calculate descriptor value for: ", descriptor.GetType().FullName);
                            Debug.WriteLine(value.GetException());
                        }
                    }
                    Debug.WriteLine("Calculated atomic descriptors...");
                }
                else if (descriptor is IBondDescriptor)
                {
                    foreach (var bond in molecule.Bonds)
                    {
                        DescriptorValue value = ((IBondDescriptor)descriptor).Calculate(bond, molecule);
                        if (value.GetException() == null)
                            bond.SetProperty(speclist[i], value);
                        else
                        {
                            Trace.TraceError("Could not calculate descriptor value for: ", descriptor.GetType().FullName);
                            Debug.WriteLine(value.GetException());
                        }
                    }
                    Debug.WriteLine("Calculated bond descriptors...");
                }
                else
                {
                    Debug.WriteLine("Unknown descriptor type for: ", descriptor.GetType().FullName);
                }
            }
        }

        /// <summary>
        /// Returns the type of the descriptor as defined in the descriptor dictionary.
        /// <para>
        /// The method will look for the identifier specified by the user in the QSAR descriptor
        /// dictionary. If a corresponding entry is found, first child element that is called
        /// "isClassifiedAs" is returned. Note that the OWL descriptor spec allows both the class of
        /// descriptor (electronic, topological etc) as well as the type of descriptor (molecular, atomic)
        /// to be specified in an "isClassifiedAs" element. Thus we ignore any such element that
        /// indicates the descriptors class.</para>
        /// <para>
        /// The method assumes that any descriptor entry will have only one "isClassifiedAs" entry describing
        /// the descriptors type.</para>
        /// <para>
        /// The descriptor can be identified either by the name of the class implementing the descriptor
        /// or else the specification reference value of the descriptor which can be obtained from an instance
        /// of the descriptor class.</para>
        /// </summary>
        /// <param name="identifier">A string containing either the descriptors fully qualified class name or else the descriptors specification reference</param>
        /// <returns>The type of the descriptor as stored in the dictionary, null if no entry is found matching the supplied identifier</returns>
        public string GetDictionaryType(string identifier)
        {
            var dictEntries = dict.GetEntries();
            string specRef = GetSpecRef(identifier);

            Debug.WriteLine("Got identifier: " + identifier);
            Debug.WriteLine("Final spec ref: " + specRef);

            foreach (var dictEntry in dictEntries)
            {
                if (!dictEntry.ClassName.Equals("Descriptor")) continue;
                if (dictEntry.Id.Equals(specRef.ToLowerInvariant()))
                {
                    XElement rawElement = (XElement)dictEntry.RawContent;
                    // Assert(rawElement != null);
                    // We're not fully Java 1.5 yet, so commented it out now. If it is
                    // really important to have it, then add @cdk.require java1.5 in the
                    // Class javadoc (and all classes that use this class)
                    var classifications = rawElement.Elements(dict.NS + "isClassifiedAs");

                    foreach (var element in classifications)
                    {
                        var attr = element.Attribute(rdfNS + "resource");
                        if ((attr.Value.IndexOf("molecularDescriptor") != -1)
                                || (attr.Value.IndexOf("atomicDescriptor") != -1))
                        {
                            string[] tmp = attr.Value.Split('#');
                            return tmp[1];
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the type of the descriptor as defined in the descriptor dictionary.
        /// <p/>
        /// The method will look for the identifier specified by the user in the QSAR descriptor
        /// dictionary. If a corresponding entry is found, first child element that is called
        /// "isClassifiedAs" is returned. Note that the OWL descriptor spec allows both the class of
        /// descriptor (electronic, topological etc) as well as the type of descriptor (molecular, atomic)
        /// to be specified in an "isClassifiedAs" element. Thus we ignore any such element that
        /// indicates the descriptors class.
        /// <p/>
        /// The method assumes that any descriptor entry will have only one "isClassifiedAs" entry describing
        /// the descriptors type.
        /// <p/>
        /// The descriptor can be identified it DescriptorSpecification object
        /// </summary>
        /// <param name="descriptorSpecification">A DescriptorSpecification object</param>
        /// <returns>the type of the descriptor as stored in the dictionary, null if no entry is found matching the supplied identifier</returns>
        public string GetDictionaryType(IImplementationSpecification descriptorSpecification)
        {
            return GetDictionaryType(descriptorSpecification.SpecificationReference);
        }

        /// <summary>
        /// Returns the class(es) of the decsriptor as defined in the descriptor dictionary.
        /// <p/>
        /// The method will look for the identifier specified by the user in the QSAR descriptor
        /// dictionary. If a corresponding entry is found, the meta-data list is examined to
        /// look for a dictRef attribute that contains a descriptorClass value. if such an attribute is
        /// found, the value of the contents attribute  add to a list. Since a descriptor may be classed in
        /// multiple ways (geometric and electronic for example), in general, a given descriptor will
        /// have multiple classes associated with it.
        /// <p/>
        /// The descriptor can be identified either by the name of the class implementing the descriptor
        /// or else the specification reference value of the descriptor which can be obtained from an instance
        /// of the descriptor class.
        ///
        /// <param name="identifier">A string containing either the descriptors fully qualified class name or else the descriptors</param>
        ///                   specification reference
        /// <returns>A List containing the names of the QSAR descriptor classes that this  descriptor was declared</returns>
        ///         to belong to. If an entry for the specified identifier was not found, null is returned.
        /// </summary>
        public string[] GetDictionaryClass(string identifier)
        {
            var dictEntries = dict.GetEntries();

            string specRef = GetSpecRef(identifier);
            if (specRef == null)
            {
                Trace.TraceError("Cannot determine specification for id: ", identifier);
                return new string[0];
            }
            List<string> dictClasses = new List<string>();

            foreach (var dictEntry in dictEntries)
            {
                if (!dictEntry.ClassName.Equals("Descriptor")) continue;
                if (dictEntry.Id.Equals(specRef.ToLowerInvariant()))
                {
                    XElement rawElement = (XElement)dictEntry.RawContent;
                    var classifications = rawElement.Elements(dict.NS + "isClassifiedAs");
                    foreach (var element in classifications)
                    {
                        var attr = element.Attribute(rdfNS + "resource");
                        if ((attr.Value.IndexOf("molecularDescriptor") >= 0)
                                || (attr.Value.IndexOf("atomicDescriptor") >= 0))
                        {
                            continue;
                        }
                        string[] tmp = attr.Value.Split('#');
                        dictClasses.Add(tmp[1]);
                    }
                }
            }

            if (dictClasses.Count == 0)
                return null;
            else
                return dictClasses.ToArray();
        }

        /// <summary>
        /// Returns the class(es) of the descriptor as defined in the descriptor dictionary.
        /// <p/>
        /// The method will look for the identifier specified by the user in the QSAR descriptor
        /// dictionary. If a corresponding entry is found, the meta-data list is examined to
        /// look for a dictRef attribute that contains a descriptorClass value. if such an attribute is
        /// found, the value of the contents attribute  add to a list. Since a descriptor may be classed in
        /// multiple ways (geometric and electronic for example), in general, a given descriptor will
        /// have multiple classes associated with it.
        /// <p/>
        /// The descriptor can be identified by its DescriptorSpecification object.
        ///
        /// <param name="descriptorSpecification">A DescriptorSpecification object</param>
        /// <returns>A List containing the names of the QSAR descriptor classes that this  descriptor was declared</returns>
        ///         to belong to. If an entry for the specified identifier was not found, null is returned.
        /// </summary>

        public string[] GetDictionaryClass(IImplementationSpecification descriptorSpecification)
        {
            return GetDictionaryClass(descriptorSpecification.SpecificationReference);
        }

        /// <summary>
        /// Gets the definition of the descriptor.
        /// <p/>
        /// All descriptors in the descriptor dictioanry will have a definition element. This function
        /// returns the value of that element. Many descriptors also have a description element which is
        /// more detailed. However the value of these elements can contain arbitrary mark up (such as MathML)
        /// and I'm not sure what I should return it as
        ///
        /// <param name="identifier">A string containing either the descriptors fully qualified class name or else the descriptors</param>
        ///                   specification reference
        /// <returns>The definition</returns>
        /// </summary>
        public string GetDictionaryDefinition(string identifier)
        {
            var dictEntries = dict.GetEntries();

            string specRef = GetSpecRef(identifier);
            if (specRef == null)
            {
                Trace.TraceError("Cannot determine specification for id: ", identifier);
                return "";
            }

            string definition = null;
            foreach (var dictEntry in dictEntries)
            {
                if (!dictEntry.ClassName.Equals("Descriptor")) continue;
                if (dictEntry.Id.Equals(specRef.ToLowerInvariant()))
                {
                    definition = dictEntry.Definition;
                    break;
                }
            }
            return definition;
        }

        /// <summary>
        /// Gets the definition of the descriptor.
        /// <p/>
        /// All descriptors in the descriptor dictioanry will have a definition element. This function
        /// returns the value of that element. Many descriptors also have a description element which is
        /// more detailed. However the value of these elements can contain arbitrary mark up (such as MathML)
        /// and I'm not sure what I should return it as
        ///
        /// <param name="descriptorSpecification">A DescriptorSpecification object</param>
        /// <returns>The definition</returns>
        /// </summary>
        public string GetDictionaryDefinition(DescriptorSpecification descriptorSpecification)
        {
            return GetDictionaryDefinition(descriptorSpecification.SpecificationReference);
        }

        /// <summary>
        /// Gets the label (title) of the descriptor.
        ///
        /// <param name="identifier">A string containing either the descriptors fully qualified class name or else the descriptors</param>
        ///                   specification reference
        /// <returns>The title</returns>
        /// </summary>
        public string GetDictionaryTitle(string identifier)
        {
            var dictEntries = dict.GetEntries();
            string specRef = GetSpecRef(identifier);
            if (specRef == null)
            {
                Trace.TraceError("Cannot determine specification for id: ", identifier);
                return "";
            }

            string title = null;
            foreach (var dictEntry in dictEntries)
            {
                if (!dictEntry.ClassName.Equals("Descriptor")) continue;
                if (dictEntry.Id.Equals(specRef.ToLowerInvariant()))
                {
                    title = dictEntry.Label;
                    break;
                }
            }
            return title;
        }

        /// <summary>
        ///  Gets the label (title) of the descriptor.
        ///
        /// <param name="descriptorSpecification">The specification object</param>
        /// <returns> The title</returns>
        /// </summary>
        public string GetDictionaryTitle(DescriptorSpecification descriptorSpecification)
        {
            return GetDictionaryTitle(descriptorSpecification.SpecificationReference);
        }

        /// <summary>
        /// Returns the DescriptorSpecification objects for all available descriptors.
        ///
        /// <returns>An array of <code>DescriptorSpecification</code> objects. These are the keys</returns>
        ///         with which the <code>DescriptorValue</code> objects can be obtained from a
        ///         molecules property list
        /// </summary>
        public IList<IImplementationSpecification> GetDescriptorSpecifications()
        {
            return speclist;
        }

        /// <summary>
        /// Set the list of <see cref="DescriptorSpecification"/> objects.
        /// </summary>
        /// <param name="specs">A list of specification objects</param>
        /// <seealso cref="GetDescriptorSpecifications"/>
        public void SetDescriptorSpecifications(IList<IImplementationSpecification> specs)
        {
            speclist = specs;
        }

        /// <summary>
        /// Returns a list containing the names of the classes implementing the descriptors.
        /// </summary>
        /// <returns>A list of class names.</returns>
        public IList<string> GetDescriptorClassNames()
        {
            return classNames;
        }

        /// <summary>
        /// Returns a List containing the instantiated descriptor classes.
        /// </summary>
        /// <returns>A List containing descriptor classes</returns>
        public IList<IDescriptor> GetDescriptorInstances()
        {
            return descriptors;
        }

        /// <summary>
        /// Set the list of <code>Descriptor</code> objects.
        /// </summary>
        /// <param name="descriptors">A List of descriptor objects</param>
        /// <see cref="GetDescriptorInstances"/>
        public void SetDescriptorInstances(IList<IDescriptor> descriptors)
        {
            this.descriptors = descriptors;
        }

        /// <summary>
        /// Get the all the unique dictionary classes that the descriptors belong to.
        /// </summary>
        /// <returns>An array containing the unique dictionary classes.</returns>
        public IEnumerable<string> GetAvailableDictionaryClasses()
        {
            List<string> classList = new List<string>();
            foreach (var spec in speclist)
            {
                string[] tmp = GetDictionaryClass(spec);
                if (tmp != null)
                {
                    foreach (var t in tmp)
                    {
                        if (!classList.Contains(t))
                        {
                            classList.Add(t);
                            yield return t;
                        }
                    }
                }
            }
            yield break;
        }

        /// <summary>
        /// Returns a list containing the classes that implement a specific interface.
        /// <p/>
        /// The interface name specified can be null or an empty string. In this case the interface name
        /// is automatcally set to <i>IDescriptor</i>.  Specifying <i>IDescriptor</i> will
        /// return all available descriptor classes. Valid interface names are
        /// <ul>
        /// <li>IMolecularDescriptor
        /// <li>IAtomicDescripto
        /// <li>IBondDescriptor
        /// <li>IDescriptor
        /// </ul>
        /// </summary>
        /// <param name="interfaceName">The name of the interface that classes should implement</param>
        /// <param name="jarFileNames">A string[] containing the fully qualified names of the jar files
        ///                      to examine for descriptor classes. In general this can be set to NULL, in which case
        ///                      the system classpath is examined for available jar files. This parameter can be set for
        ///                      situations where the system classpath is not available or is modified such as in an application
        ///                      container.</param>
        /// <returns>A list containing the classes implementing the specified interface, null if an invalid interface is specified</returns>
        public static IEnumerable<string> GetDescriptorClassNameByInterface(string interfaceName, IEnumerable<Assembly> assemblies)
        {
            if (string.IsNullOrEmpty(interfaceName))
                interfaceName = "IDescriptor";
            switch (interfaceName)
            {
                case "IDescriptor":
                case "IMolecularDescriptor":
                case "IAtomicDescriptor":
                case "IBondDescriptor":
                    break;
                default:
                    yield break;
            }
            var interface_ = typeof(DescriptorEngine).Assembly.GetType(interfaceName);

            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    if (interface_.IsAssignableFrom(type))
                        yield return type.FullName;
                }
            }
            yield break;
        }

        /// <summary>
        /// Returns a list containing the classes found in the specified descriptor package.
        /// <para>
        /// The package name specified can be null or an empty string. In this case the package name
        /// is automatcally set to "NCDK.QSAR.Descriptors" and as a result will return
        /// classes corresponding to both atomic and molecular descriptors.
        /// </para>
        /// </summary>
        /// <param name="packageName">The name of the package containing the required descriptor</param>
        /// <param name="jarFileNames">A string[] containing the fully qualified names of the jar files
        ///                     to examine for descriptor classes. In general this can be set to NULL, in which case
        ///                     the system classpath is examined for available jar files. This parameter can be set for
        ///                     situations where the system classpath is not available or is modified such as in an application
        ///                     container.</param>
        /// <returns>A list containing the classes in the specified package</returns>
        public static IEnumerable<string> GetDescriptorClassNameByPackage(string packageName, IEnumerable<Assembly> assemblies)
        {
            if (string.IsNullOrEmpty(packageName))
                packageName = typeof(DescriptorEngine).Namespace + ".Descriptors";

            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.FullName.StartsWith(packageName))
                    {
                        if (type.IsNested) continue;
                        if (type.FullName.Contains("Test")) continue;
                        if (type.FullName.Contains("ChiIndexUtils")) continue;
                        yield return type.FullName;
                    }
                }
            }
            yield break;
        }

        public List<IDescriptor> InstantiateDescriptors(IEnumerable<string> descriptorClassNames)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var descriptors = new List<IDescriptor>();
            foreach (var descriptorName in descriptorClassNames)
            {
                try
                {
                    var c = assembly.GetType(descriptorName);	// c is IDescriptor type
                    IDescriptor descriptor = Instantiate(c);
                    descriptor.Initialise(builder);
                    descriptors.Add(descriptor);
                    Trace.TraceInformation("Loaded descriptor: ", descriptorName);
                }
                catch (Exception error)
                {
                    Trace.TraceError($"Could not find this Descriptor: {descriptorName}");
                    Debug.WriteLine(error);
                }
            }
            return descriptors;
        }

        private IDescriptor Instantiate(Type c)
        {
            ConstructorInfo ctor;
            ctor = c.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
                return (IDescriptor)ctor.Invoke(Array.Empty<object>());
            ctor = c.GetConstructor(new Type[] { typeof(IChemObjectBuilder) });
            if (ctor != null)
                return (IDescriptor)ctor.Invoke(new Type[] { typeof(IChemObjectBuilder) });

            throw new InvalidOperationException($"descriptor {c.Name} has no usable constructors");
        }

        public IEnumerable<IImplementationSpecification> InitializeSpecifications(IEnumerable<IDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
				yield return descriptor.Specification;
            }
            yield break;
        }

        private string GetSpecRef(string identifier)
        {
            string specRef = null;
            // see if we got a descriptors java class name
            for (int i = 0; i < classNames.Count; i++)
            {
                string className = classNames[i];
                if (className.Equals(identifier))
                {
                    IDescriptor descriptor = descriptors[i];
                    IImplementationSpecification descSpecification = descriptor.Specification;
                    string[] tmp = descSpecification.SpecificationReference.Split('#');
                    if (tmp.Length != 2)
                    {
                        Debug.WriteLine("Something fishy with the spec ref: ", descSpecification.SpecificationReference);
                    }
                    else
                    {
                        specRef = tmp[1];
                    }
                }
            }
            // if we are here and specRef==null we have a SpecificationReference
            if (specRef == null)
            {
                string[] tmp = identifier.Split('#');
                if (tmp.Length != 2)
                {
                    Debug.WriteLine("Something fishy with the identifier: ", identifier);
                }
                else
                {
                    specRef = tmp[1];
                }
            }
            return specRef;
        }
    }
}

